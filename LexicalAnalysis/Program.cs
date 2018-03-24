using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexicalAnalysis
{
	class Program
	{
		static Symbol[] ReadSymbols(String filePath)
		{
			List<Symbol> list = new List<Symbol>();
			try
			{
				FileStream fs = new FileStream(filePath, FileMode.Open);
				StreamReader rs = new StreamReader(fs);
				int code = 1;
				while (!rs.EndOfStream)
				{
					if (code == 11) code = 13;
					String str = rs.ReadLine();
					list.Add(new Symbol(str, code++));
				}
				list.Add(new Symbol("\n", code++));
				rs.Close();
				fs.Close();
			}
			catch (Exception e) { throw e; }
			return list.ToArray();
		}
		static String ReadCode(String filePath)
		{
			String str;
			try
			{
				FileStream fs = new FileStream(filePath, FileMode.Open);
				StreamReader rs = new StreamReader(fs);
				str = rs.ReadToEnd();
				rs.Close();
				fs.Close();
			}
			catch (Exception e) { throw e; }

			str=str.Replace('\t', ' ');
			str=str.Replace('\r', ' ');

			return str;
		}
		static void WriteFile(dynamic content)
		{

		}
		static void Main(string[] args)
		{
			Symbol[] symbols;
			String code;
			try
			{
				symbols = ReadSymbols(@"../../../src/symbol.txt");
				code = ReadCode(@"../../../src/code.txt");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return;
			}
			LexicalAnalysisMachine machine = new LexicalAnalysisMachine(symbols);
			machine.Run(code);
		}
	}
	class LexicalAnalysisMachine
	{
		class WordType
		{
			public const int Error = 0;
			public const int KeyWord = 1;
			public const int Number = 2;
			public const int Signal = 3;
			public const int IDentifier = 4;
		}
		private char[] markList;
		private Symbol[] symbols;
		public LexicalAnalysisMachine(Symbol[] symbols)
		{
			this.symbols = symbols;
			var charSet = new HashSet<char>();
			foreach (var symbol in symbols)
			{
				for (int i = 0; i < symbol.content.Length; i++)
					if (!isLetter(symbol.content[i]))
						charSet.Add(symbol.content[i]);
			}
			markList = charSet.ToArray();
		}
		bool isLetter(char c)
		{
			if (('a' <= c && c <= 'z') || 'A' <= c && c <= 'Z')
				return true;
			return false;
		}
		bool isDigit(char c)
		{
			if ('0' <= c && c <= '9')
				return true;
			return false;
		}
		bool isMark(char c)
		{
			foreach (char m in markList)
				if (m == c)
					return true;
			return false;
		}
		int GetSymbolClassCode(String s)
		{
			foreach (var symbol in symbols)
				if (symbol.content.CompareTo(s) == 0)
					return symbol.classCode;
			return 0;
		}
		String[] sourceCodeLines;

		private String[] Prase(String str)
		{
			var stringBuilder = new StringBuilder();
			var list = new List<string>();
			for (int i = 0; i < str.Length; i++)
			{
				if (str[i] != ' ')
				{
					stringBuilder.Append(str[i]);
					if (i + 1 == str.Length || str[i + 1] == ' ')
					{
						list.Add(stringBuilder.ToString());
						stringBuilder.Clear();
					}
				}
			}
			return list.ToArray();
		}
		class Error
		{
			String errorWord;
			int lineNumber;
			String errorDescribe;
			public Error(String errorWord, int lineNumber)
			{
				this.errorWord = errorWord;
				this.lineNumber = lineNumber;
				this.errorDescribe = String.Format("Detected a invalid word \"{0}\" on line {1}.", errorWord, lineNumber);
			}
			override public String ToString()
			{
				return errorDescribe;
			}
		}
		int WordAnalyse(String s)
		{
			int state = 0;
			int i = 0;
			while (true)
			{
				if(i==s.Length||state==5)
				{
					break;
				}
				switch (state)
				{
					case 0:
						if (isDigit(s[i])) state = 1;
						else if (isLetter(s[i])) state = 3;
						else if (isMark(s[i])) state = 4;
						else state = 5;
						break;
					case 1:
						if (isDigit(s[i])) state = 1;
						else if (s[i] == '.') state = 2;
						else state = 5;
						break;
					case 2:
						if (isDigit(s[i])) state = 2;
						else state = 5;
						break;
					case 3:
						if (isLetter(s[i])||isDigit(s[i])) state = 3;
						else state = 5;
						break;
					case 4:
						if (isMark(s[i])) state = 4;
						else state = 5;
						break;
				}
				i++;
			}
			switch(state)
			{
				case 1:
				case 2:
					return WordType.Number;
				case 3:
					if (GetSymbolClassCode(s) != 0)
						return WordType.KeyWord;
					else
						return WordType.IDentifier;
				case 4:
					if (GetSymbolClassCode(s) != 0)
						return WordType.Signal;
					break;
			}
			return WordType.Error;
		}
		public void Run(String content)
		{
			var errors = new Queue<Error>();
			var tokens = new Queue<Symbol>();
			var iDentifierSet = new HashSet<String>();
			var numberSet = new HashSet<string>();
			sourceCodeLines = content.Split('\n');
			for (int i = 0; i < sourceCodeLines.Length; i++)
			{
				String[] words = Prase(sourceCodeLines[i]);
				foreach (var word in words)
				{
					int type = WordAnalyse(word);
					switch (type)
					{
						case WordType.Error:
							errors.Enqueue(new Error(word, i));
							break;
						case WordType.KeyWord:
							tokens.Enqueue(new Symbol(word, GetSymbolClassCode(word)));
							break;
						case WordType.IDentifier:
							tokens.Enqueue(new Symbol(word, Symbol.IDentifierCode));
							iDentifierSet.Add(word);
							break;
						case WordType.Number:
							tokens.Enqueue(new Symbol(word, Symbol.NumberCode));
							numberSet.Add(word);
							break;
						case WordType.Signal:
							tokens.Enqueue(new Symbol(word, GetSymbolClassCode(word)));
							break;
					}
				}
			}
			
			/*
			while(tokens.Count!=0)
				Console.WriteLine(tokens.Dequeue().content);
	
			while(errors.Count!=0)
				Console.WriteLine(errors.Dequeue());

			foreach(var item in iDentifierSet)
				Console.WriteLine(item);
			foreach (var item in numberSet)
				Console.WriteLine(item);
			*/
		}
	}
	class Symbol
	{
		public const int IDentifierCode = 11;
		public const int NumberCode = 12;
		public Symbol(String content, int classCode)
		{
			this.content = content;
			this.classCode = classCode;
		}
		public String content;
		public int classCode;
	}
}

