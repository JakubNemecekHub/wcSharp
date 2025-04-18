using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

namespace wc;

class Program
{
	static int Main(string[] args)
	{
		var rootCommand = new RootCommand("wc implementation in C#");

		var filesArgument = new Argument<string[]>(
			name: "--files",
			description: "Files to process."
		);

		var linesOption = new Option<bool>(
			aliases: ["-l", "--lines"],
			description: "print the newline counts",
			getDefaultValue: () => false
		);

		var wordsOption = new Option<bool>(
			aliases: ["-w", "--words"],
			description: "print the word counts",
			getDefaultValue: () => false
		);

		var bytesOption = new Option<bool>(
			aliases: ["-c", "--bytes"],
			description: "print the byte counts",
			getDefaultValue: () => false
		);

		var charsOption = new Option<bool>(
			aliases: ["-m", "--chars"],
			description: "print the character counts",
			getDefaultValue: () => false
		);

		rootCommand.AddArgument(filesArgument);
		rootCommand.AddOption(linesOption);
		rootCommand.AddOption(wordsOption);
		rootCommand.AddOption(bytesOption);
		rootCommand.AddOption(charsOption);

		rootCommand.Handler = CommandHandler.Create<string[], bool, bool, bool, bool>(
			(files, lines, words, bytes, chars) => Wc(files, lines, words, bytes, chars)
		);

		return rootCommand.Invoke(args);

	}

	/*
		Flag struct holding the boolean values of flag arguments.
	*/
	private struct Flags(bool lines, bool words, bool bytes, bool characters)
	{
		public bool lines = lines;
		public bool words = words;
		public bool bytes = bytes;
		public bool characters = characters;

		public readonly bool Any()
		{
			return lines || words || bytes || characters;
		}
	}

	/*
		Struct holding the results of a count.
	*/
	private struct Count(int lines, int words, int bytes, int characters)
    {
		public int lines = lines;
		public int words = words;
		public int bytes = bytes;
		public int characters = characters;

		public static Count operator +(Count a, Count b)
		{
			return new Count(a.lines + b.lines, a.words + b.words,
			                 a.bytes + b.bytes, a.characters + b.characters);
		}
    }

	static void Print(Count result, Flags flags)
	{
		if (flags.lines || !flags.Any()) Console.Write($"{result.lines} ");
		if (flags.words || !flags.Any()) Console.Write($"{result.words} ");
		if (flags.bytes || !flags.Any()) Console.Write($"{result.bytes} ");
		if (flags.characters)	         Console.Write($"{result.characters} ");
	}

	static void Wc(string[] files, bool lines, bool words, bool bytes, bool characters)
	{
		var missingFiles = new List<string>();

		var flags = new Flags(lines, words, bytes, characters);

		var totalResult = new Count(0, 0, 0, 0);
		var result = new Count(0, 0, 0, 0);

		if (files.Length == 0)
		{
			result = ProcessInput(Console.OpenStandardInput());
			Print(result, flags);
		}
		else
		{
			foreach (var file in files)
			{
				if (!File.Exists(file))
				{
					missingFiles.Add(file);
					continue;
				}
				using var inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);
				result = ProcessInput(inputStream);
				totalResult += result;
				Print(result, flags);
			}
			if (files.Length > 1) Print(totalResult, flags);
			foreach (var file in missingFiles)
			{
				Console.WriteLine($"File \"{file}\" not found.");
			}
		}
	}

	private static bool IsContinuation(int character)
	{
		return (character & 0xC0) == 0x80;
	}

    private static Count ProcessInput(Stream inputStream)
	{
		int c = inputStream.ReadByte();
		if (c == -1) return new Count(0, 0, 0, 0);

		var count = new Count(1, 0, 0, 0);

		bool flagPrevious = Char.IsWhiteSpace((char)c);
		bool flagCurrent = flagPrevious;

		do {
			// lines
			if (c == '\n') ++count.lines;
			// words
			flagCurrent = Char.IsWhiteSpace((char)c);
			if (flagCurrent && !flagPrevious) ++count.words;
			flagPrevious = flagCurrent;
			// bytes
			++count.bytes;
			// characters
			if (!IsContinuation(c)) ++count.characters;
		} while ((c = inputStream.ReadByte()) != -1);
		if (!flagCurrent) ++count.words;
		return count;
	}

}
