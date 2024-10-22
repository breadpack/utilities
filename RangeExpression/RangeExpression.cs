using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BreadPack.Utilities {
    public class RangeExpression {
        private struct Range {
            private int? start { get; }
            private int? end   { get; }

            public Range(int? start, int? end) {
                this.start = start;
                this.end   = end;
            }

            public bool IsMatch(int value) {
                return (start == null || value >= start)
                    && (end == null || value < end);
            }
        }

        private readonly List<Range> _ranges = new();

        private Regex _regex = new(
            @"(?<begin>-?\d+)?\.\.(?<end>-?\d+)?|(?<value>-?\d+)|\*"
          , RegexOptions.Singleline | RegexOptions.Compiled
        );

        public RangeExpression(string pattern) {
            pattern = pattern.Replace(" ", "");
            var parts = pattern.Split(',');
            if (parts == null || !parts.Any())
                throw new ArgumentException("Invalid pattern - " + pattern, nameof(pattern));

            foreach (var part in parts) {
                var m = _regex.Match(part);
                if (m.Value != part)
                    throw new ArgumentException("Invalid pattern - " + pattern, nameof(pattern));
                if (!m.Success)
                    throw new ArgumentException("Invalid pattern - " + pattern, nameof(pattern));
                
                if (m.Groups["value"].Success) {
                    var value = int.Parse(m.Groups["value"].Value);
                    _ranges.Add(new(value, value + 1));
                } else if (m.Groups["begin"].Success && m.Groups["end"].Success) {
                    var begin = int.Parse(m.Groups["begin"].Value);
                    var end   = int.Parse(m.Groups["end"].Value);
                    _ranges.Add(new(begin, end + 1));
                } else if (m.Groups["begin"].Success) {
                    var begin = int.Parse(m.Groups["begin"].Value);
                    _ranges.Add(new(begin, null));
                } else if (m.Groups["end"].Success) {
                    var end = int.Parse(m.Groups["end"].Value);
                    _ranges.Add(new(null, end + 1));
                } else if (m.Groups[0].Value == "*") {
                    _ranges.Add(new(null, null));
                }
            }
        }

        public bool IsMatch(int value) {
            return _ranges.Any(range => range.IsMatch(value));
        }
    }
}