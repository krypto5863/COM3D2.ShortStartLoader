using System.Text;
using System.Text.RegularExpressions;

namespace ShortStartLoader
{
	internal static class WildcardToRegex
	{
		private static readonly char[] Wildcards = { '*', '?', '.' };

		public static Regex WildcardToRegexp(string wExp)
		{
			if (string.IsNullOrEmpty(wExp))
			{
				return null;
			}

			var regexp = new StringBuilder();

			for (var startIndex = 0; startIndex < wExp.Length;)
			{
				var i = wExp.IndexOfAny(Wildcards, startIndex);

				if (i == -1)
				{
					// no wildcards found, append the remaining characters to the regex
					regexp.Append(wExp, startIndex, wExp.Length - startIndex);
					break;
				}

				// append all non wilcard characters
				regexp.Append(wExp, startIndex, i - startIndex);

				// convert the wildcard to regex
				if (wExp[i] == '.')
				{
					regexp.Append("\\.");
				}

				if (wExp[i] == '*')
				{
					regexp.Append(".*");
				}
				else if (wExp[i] == '?')
				{
					regexp.Append('.');
				}

				startIndex = i + 1;
			}

			return new Regex(regexp.ToString(), RegexOptions.Singleline);
		}
	}
}