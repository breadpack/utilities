using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BreadPack.Utilities {
    [Serializable]
    public class RangeExpression<T> where T : IComparable<T> {

        private enum RangeType {
            StartOnly,
            EndOnly,
            Both
        }
        
        [Serializable]
        private readonly struct Range {
            private readonly RangeType _type;
            private readonly T _start;
            private readonly T _end;

            public T? Start => _start;
            public T? End => _end;
            
            public Range(RangeType type, T start, T end) {
                _type = type;
                _start = start;
                _end = end;
                
                if(type == RangeType.Both && start.CompareTo(end) > 0)
                    throw new ArgumentException("Start value must be less than or equal to end value.");
            }

            public bool Contains(T value) {
                switch (_type) {
                    case RangeType.StartOnly:
                        return value.CompareTo(_start) >= 0;
                    case RangeType.EndOnly:
                        return value.CompareTo(_end) <= 0;
                    case RangeType.Both:
                        return value.CompareTo(_start) >= 0 && value.CompareTo(_end) <= 0;
                    default:
                        throw new InvalidOperationException("Invalid range type.");
                }
            }
        }

        private readonly string _pattern;
        [NonSerialized]
        private List<Range> _ranges;
        [NonSerialized]
        private Regex _regex;
        [NonSerialized]
        private Func<string, T> _parser;
        [NonSerialized]
        private Func<T, T> _getNextValue;

        // 정적 Regex 캐시
        private static readonly Dictionary<Type, Regex> RegexCache = new();
        
        private static Regex GetOrCreateRegex(Type type) {
            if (RegexCache.TryGetValue(type, out var regex))
                return regex;
                
            var valuePattern = GetDefaultValuePattern(type);
            regex = new Regex(
                $@"(?<begin>{valuePattern})?\.\.(?<end>{valuePattern})?|(?<value>{valuePattern})",
                RegexOptions.Singleline | RegexOptions.Compiled
            );
            RegexCache[type] = regex;
            return regex;
        }

        private static string GetDefaultValuePattern(Type type) {
            if (type == typeof(int) || type == typeof(long))
                return @"-?\d+";
            if (type == typeof(DateTime))
                return @"\d{4}-\d{2}-\d{2}(?:T\d{2}:\d{2}:\d{2}(?:\.\d+)?)?(?:Z|[-+]\d{2}:?\d{2})?";
            if (type == typeof(TimeSpan))
                return @"\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return @"-?\d+\.?\d*";
            
            return @"[^,\s]+";  // 기본 패턴: 쉼표와 공백을 제외한 모든 문자
        }

        private static Func<T, T> GetDefaultNextValueFunc() {
            if (typeof(T) == typeof(int))
                return value => (T)(object)((int)(object)value + 1);
            if (typeof(T) == typeof(long))
                return value => (T)(object)((long)(object)value + 1);
            if (typeof(T) == typeof(double))
                return value => (T)(object)((double)(object)value + 1);
            if (typeof(T) == typeof(float))
                return value => (T)(object)((float)(object)value + 1);
            if (typeof(T) == typeof(DateTime))
                return value => (T)(object)((DateTime)(object)value).AddDays(1);
            return value => value;
        }

        private static Func<string, T> GetDefaultParser() {
            if (typeof(T) == typeof(int)) return s => (T)(object)int.Parse(s);
            if (typeof(T) == typeof(long)) return s => (T)(object)long.Parse(s);
            if (typeof(T) == typeof(double)) return s => (T)(object)double.Parse(s);
            if (typeof(T) == typeof(float)) return s => (T)(object)float.Parse(s);
            if (typeof(T) == typeof(DateTime)) return s => (T)(object)DateTime.Parse(s);
            if (typeof(T) == typeof(TimeSpan)) 
            {
                return s => {
                    if (TimeSpan.TryParse(s, out TimeSpan result))
                        return (T)(object)result;
                    throw new FormatException($"Invalid TimeSpan format: {s}");
                };
            }
            throw new InvalidOperationException($"No default parser available for type {typeof(T)}");
        }

        public RangeExpression(string pattern) : this(pattern, null, null) { }

        public RangeExpression(string pattern, Func<string, T>? parser = null, Func<T, T>? nextValueFunc = null) {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));

            _pattern = pattern;
            _parser = parser ?? GetDefaultParser();
            _getNextValue = nextValueFunc ?? GetDefaultNextValueFunc();
            _regex = GetOrCreateRegex(typeof(T));
            _ranges = ParseRanges(_pattern);
        }

        private List<Range> ParseRanges(ReadOnlySpan<char> pattern) {
            // 초기 콤마 개수로 List 용량 예측
            int rangeCount = 1;
            for (int i = 0; i < pattern.Length; i++)
                if (pattern[i] == ',') rangeCount++;
                
            var ranges = new List<Range>(rangeCount);
            
            int currentStart = 0;
            for (int i = 0; i <= pattern.Length; i++) {
                if (i == pattern.Length || pattern[i] == ',') {
                    var part = pattern.Slice(currentStart, i - currentStart).Trim();
                    if (!part.IsEmpty) {
                        ParseRange(part, ranges);
                    }
                    currentStart = i + 1;
                }
            }
            return ranges;
        }

        private void ParseRange(ReadOnlySpan<char> part, List<Range> ranges) {
            // ".." 검색
            int dotIndex = part.IndexOf("..");
            
            if (dotIndex >= 0) {
                var beforeDot = part.Slice(0, dotIndex).Trim();
                var afterDot = part.Slice(dotIndex + 2).Trim();

                if (beforeDot.IsEmpty && afterDot.IsEmpty) {
                    throw new ArgumentException("Either start or end value must be specified.");
                }
                
                if (beforeDot.IsEmpty) {
                    var end = _parser(afterDot.ToString());
                    ranges.Add(new Range(RangeType.EndOnly, default, end));
                }
                else if (afterDot.IsEmpty) {
                    var start = _parser(beforeDot.ToString());
                    ranges.Add(new Range(RangeType.StartOnly, start, default));
                }
                else {
                    var start = _parser(beforeDot.ToString());
                    var end = _parser(afterDot.ToString());
                    ranges.Add(new Range(RangeType.Both, start, end));
                }
            }
            else {
                var value = _parser(part.ToString());
                ranges.Add(new Range(RangeType.Both, value, value));
            }
        }

        private string GetPatternErrorMessage(string pattern) {
            if (string.IsNullOrWhiteSpace(pattern))
                return "Empty pattern is not allowed.";
            
            if (pattern == "..")
                return "Either start or end value must be specified.";
            
            if (pattern.Count(c => c == '.') > 2)
                return "Range separator ('..') is used incorrectly.";
            
            if (pattern.Contains("..")) {
                var parts = pattern.Split(new[] { ".." }, StringSplitOptions.None);
                if (parts.Length != 2)
                    return "Invalid range format. Must be in 'start..end' format.";
                
                if (string.IsNullOrEmpty(parts[0]) && string.IsNullOrEmpty(parts[1]))
                    return "Either start or end value must be specified.";
            }
            
            var valuePattern = GetDefaultValuePattern(typeof(T));
            if (!new Regex($"^({valuePattern})$").IsMatch(pattern))
                return $"Value is not in the correct format. Expected format: {GetTypeFormatExample(typeof(T))}";
            
            return "An unknown error occurred.";
        }

        private static string GetTypeFormatExample(Type type) {
            if (type == typeof(int) || type == typeof(long))
                return "integer (e.g., -123, 0, 456)";
            if (type == typeof(DateTime))
                return "date (e.g., 2024-01-01)";
            if (type == typeof(TimeSpan))
                return "time (e.g., 00:00:00)";
            if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                return "decimal (e.g., -1.23, 0.0, 4.56)";
            return "string";
        }

        public bool Contains(T value) {
            foreach (var range in _ranges) {
                if (range.Contains(value))
                    return true;
            }
            return false;
        }

        public bool IsMatch(T value) => Contains(value);

        public IEnumerable<(T? Start, T? End)> GetRanges() {
            return _ranges.Select(r => (r.Start, r.End));
        }

        public override string ToString() => _pattern;
    }
}