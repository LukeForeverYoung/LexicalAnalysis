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
			str=System.Text.RegularExpressions.Regex.Replace(str, "\r", "");

			return str;
		}
		static void WriteTokens(String fileName, Queue<Symbol> tokens)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			StringBuilder stringBuilder = new StringBuilder();
			foreach(var token in tokens)
			{
				stringBuilder.Append(token.content+" "+token.classCode);
				stringBuilder.AppendLine();
			}
			sw.Write(stringBuilder);
			sw.Close();
			fs.Close();
		}
		static void WriteErrors(String fileName, Queue<Error> errors )
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var error in errors)
			{
				stringBuilder.Append(error);
				stringBuilder.AppendLine();
			}
			sw.Write(stringBuilder);
			sw.Close();
			fs.Close();
		}
		static void WriteIDentifier(String fileName, HashSet<String> iDentifierSet)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var iDentifier in iDentifierSet)
			{
				stringBuilder.Append(iDentifier);
				stringBuilder.AppendLine();
			}
			sw.Write(stringBuilder);
			sw.Close();
			fs.Close();
		}
		static void WriteNumber(String fileName, HashSet<String> numberSet)
		{
			FileStream fs = new FileStream(fileName, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);
			StringBuilder stringBuilder = new StringBuilder();
			foreach (var number in numberSet)
			{
				stringBuilder.Append(number);
				stringBuilder.AppendLine();
			}
			sw.Write(stringBuilder);
			sw.Close();
			fs.Close();
		}
		static void Main(string[] args)
		{
			Symbol[] symbols;
			String code;
			try
			{
				symbols = ReadSymbols(@"src/symbol.txt");
				code = ReadCode(@"src/code.txt");
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return;
			}
			LexicalAnalysisMachine machine = new LexicalAnalysisMachine(symbols);
			Queue<Error> errors;
			Queue<Symbol> tokens;
			HashSet<String> iDentifierSet;
			HashSet<String> numberSet;
			machine.Run(code, out errors, out tokens, out iDentifierSet, out numberSet);
			WriteTokens(@"out/tokens.txt",tokens);
			WriteErrors(@"out/errors.txt",errors);
			WriteIDentifier(@"out/iDentifier.txt",iDentifierSet);
			WriteNumber(@"out/number.txt",numberSet);
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
			public const int Nothing = 5;//处理行末空格的情况
		}
		private char[] markList;
		private Symbol[] symbols;
		private Symbol CRLF,EOF;
		public LexicalAnalysisMachine(Symbol[] symbols)
		{
			EOF = new Symbol("EOF-1", -1);
			this.symbols = symbols;
			var charSet = new HashSet<char>();
			foreach (var symbol in symbols)
			{
				for (int i = 0; i < symbol.content.Length; i++)
				{
					if (!isLetter(symbol.content[i]))
						charSet.Add(symbol.content[i]);
				}
				if (symbol.content == "\n")
					CRLF = symbol;
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

		/*
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
		*/
		/// <summary>
		/// 分析一行字符串
		/// state:1/2/6 数字 , 3/8 标识符串 , 4/9符号串 , 5 错误
		/// 大于5的state表示下一位读到了和当前类型不兼容的字符，做截停操作。
		/// 5代表读到了非法字符，做报错操作。
		/// </summary>
		/// <param name="s">字符串引用</param>
		/// <param name="index">起点坐标</param>
		/// <param name="word">返回分析结果</param>
		/// <returns>返回Type，定义在WordType中</returns>
		int WordAnalyse(String s,ref int index,out String word)
		{
			while (index != s.Length && s[index] == ' ') index++;
			if(index==s.Length)
			{
				word = "";
				return WordType.Nothing;
			}
			int startPosition = index;
			int state = 0;
			StringBuilder stringBuilder = new StringBuilder();
			//Console.WriteLine(s);
			//Console.WriteLine(index);
			while (true)
			{
				if (index == s.Length) break;
				switch (state)
				{
					case 0:
						if (isDigit(s[index])) state = 1;
						else if (isLetter(s[index])) state = 3;
						else if (isMark(s[index])) state = 4;
						else state = 5;
						break;
					case 1:
						if (isDigit(s[index])) state = 1;
						else if (s[index] == '.') state = 2;
						else state = 6;
						break;
					case 2:
						if (isDigit(s[index])) state = 2;
						else if (s[index] == '.') state = 6;
						else state = 6;
						break;
					case 3:
						if (isLetter(s[index])||isDigit(s[index])) state = 3;
						else state = 8;
						break;
					case 4:
						if (isMark(s[index])&&GetSymbolClassCode(s.Substring(startPosition,index-startPosition+1))!=0) state = 4;
						else state = 9;
						break;
				}
				if (state >= 5) break;
				index++;
			}
			if(state==5)
			{
				word = s.Substring(startPosition, index - startPosition + 1);
				index++;
			}
			else if (state > 5)
			{
				word = s.Substring(startPosition, index - startPosition);
				state -= 5;
			}
			else
			{
				word= s.Substring(startPosition);
				index++;
			}
			switch (state)
			{
				case 1:
				case 2:
					return WordType.Number;
				case 3:
					if (GetSymbolClassCode(word) != 0)
						return WordType.KeyWord;
					else
						return WordType.IDentifier;
				case 4:
					if (GetSymbolClassCode(word) != 0)
						return WordType.Signal;
					break;
			}
			return WordType.Error;
		}
		public void Run(String content,out Queue<Error> errors,out Queue<Symbol> tokens,out HashSet<String> iDentifierSet,out HashSet<String> numberSet)
		{
			errors = new Queue<Error>();
			tokens = new Queue<Symbol>();
			iDentifierSet = new HashSet<string>();
			numberSet = new HashSet<string>();
			sourceCodeLines = content.Split('\n');
			for (int i = 0; i < sourceCodeLines.Length; i++)
			{
				int index = 0;
				while(index<sourceCodeLines[i].Length)
				{
					String word;
					int type = WordAnalyse(sourceCodeLines[i],ref index,out word);
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
						case WordType.Nothing:
							break;
					}
				}
				tokens.Enqueue(CRLF);
			}
			tokens.Enqueue(EOF);
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

