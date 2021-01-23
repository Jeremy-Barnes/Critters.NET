using System;
using System.Text;

namespace jabarnes.Metaphone
{
	/// <summary>
	///     C# implementation of Lawrence Phillips' Double Metaphone algorithm suggested optimization, whereby
	///     four-letter metaphone keys are represented as four nibbles in an unsigned short to improve storage/search efficiency
	///     Ported to .NET Standard by Jeremy Barnes
	/// </summary>
	public class ShortDoubleMetaphone : DoubleMetaphone
	{
		//Constants representing the characters in a metaphone key
		public const ushort METAPHONE_A = 0x01;
		public const ushort METAPHONE_F = 0x02;
		public const ushort METAPHONE_FX = ((METAPHONE_F << 4) | METAPHONE_X);
		public const ushort METAPHONE_H = 0x03;
		public const ushort METAPHONE_J = 0x04;
		public const ushort METAPHONE_K = 0x05;
		public const ushort METAPHONE_KL = ((METAPHONE_K << 4) | METAPHONE_L);
		public const ushort METAPHONE_KN = ((METAPHONE_K << 4) | METAPHONE_N);
		public const ushort METAPHONE_KS = ((METAPHONE_K << 4) | METAPHONE_S);
		public const ushort METAPHONE_L = 0x06;
		public const ushort METAPHONE_M = 0x07;
		public const ushort METAPHONE_N = 0x08;
		public const ushort METAPHONE_P = 0x09;
		public const ushort METAPHONE_S = 0x0A;
		public const ushort METAPHONE_SK = ((METAPHONE_S << 4) | METAPHONE_K);
		public const ushort METAPHONE_T = 0x0B;
		public const ushort METAPHONE_TK = ((METAPHONE_T << 4) | METAPHONE_K);
		public const ushort METAPHONE_TS = ((METAPHONE_T << 4) | METAPHONE_S);
		public const ushort METAPHONE_R = 0x0C;
		public const ushort METAPHONE_X = 0x0D;
		public const ushort METAPHONE_0 = 0x0E;
		public const ushort METAPHONE_SPACE = 0x0F;
		public const ushort METAPHONE_NULL = 0x00;

		/// Sentinel value, used to denote an invalid key
		public const ushort METAPHONE_INVALID_KEY = 0xffff;

		/// <summary>
		/// Default ctor, initializes to an empty string and 0 keys
		/// </summary>
		public ShortDoubleMetaphone() : base("")
		{
			PrimaryShortKey = AlternateShortKey = 0;
		}

		/// <summary>
		/// Computes ushort representations of the metaphone keys
		/// </summary>
		/// 
		/// <param name="word">Word for which to compute metaphone keys</param>
		public ShortDoubleMetaphone(string word) : base(word)
		{
			PrimaryShortKey = MetaphoneKeyToShort(PrimaryKey);
			if (this.AlternateKey != null)
			{
				AlternateShortKey = MetaphoneKeyToShort(AlternateKey);
			}
			else
			{
				AlternateShortKey = METAPHONE_INVALID_KEY;
			}
		}

		/// <summary>
		/// Sets a new current word, computing the string and ushort representations of the metaphone keys of the given word.
		/// Note: that this uses the new keyword, avoiding virtual methods for performance
		/// </summary>
		/// 
		/// <param name="word">New current word for which to compute metaphone keys</param>
		public new void ComputeKeys(string word)
		{
			base.ComputeKeys(word);

			PrimaryShortKey = MetaphoneKeyToShort(this.PrimaryKey);
			if (this.AlternateKey != null)
			{
				AlternateShortKey = MetaphoneKeyToShort(this.AlternateKey);
			}
			else
			{
				AlternateShortKey = METAPHONE_INVALID_KEY;
			}
		}

		/// <summary>The primary metaphone key, represented as a ushort</summary>
		public ushort PrimaryShortKey
		{
			get; internal set;
		}

		/// <summary>The alternative metaphone key, or METAPHONE_INVALID_KEY if the current
		///     word has no alternate key by double metaphone</summary>
		public ushort AlternateShortKey
		{
			get; internal set;
		}

		/// <summary>Represents a string metaphone key as a ushort</summary>
		/// 
		/// <param name="metaphoneKey">String metaphone key.  Must be four chars long; if you change METAPHONE_KEY_LENGTH in DoubleMetaphone, this will break.  
		/// Length tests are not performed, for performance reasons.</param>
		/// <returns>ushort representation of the given metahphone key</returns>
		private static ushort MetaphoneKeyToShort(string metaphoneKey)
		{
			ushort result, charResult;
			char currentChar;

			result = 0;

			for (int i = 0; i < metaphoneKey.Length; i++)
			{
				currentChar = metaphoneKey[i];
				if (currentChar == 'A')
					charResult = METAPHONE_A;
				else if (currentChar == 'P')
					charResult = METAPHONE_P;
				else if (currentChar == 'S')
					charResult = METAPHONE_S;
				else if (currentChar == 'K')
					charResult = METAPHONE_K;
				else if (currentChar == 'X')
					charResult = METAPHONE_X;
				else if (currentChar == 'J')
					charResult = METAPHONE_J;
				else if (currentChar == 'T')
					charResult = METAPHONE_T;
				else if (currentChar == 'F')
					charResult = METAPHONE_F;
				else if (currentChar == 'N')
					charResult = METAPHONE_N;
				else if (currentChar == 'H')
					charResult = METAPHONE_H;
				else if (currentChar == 'M')
					charResult = METAPHONE_M;
				else if (currentChar == 'L')
					charResult = METAPHONE_L;
				else if (currentChar == 'R')
					charResult = METAPHONE_R;
				else if (currentChar == ' ')
					charResult = METAPHONE_SPACE;
				else if (currentChar == '\0')
					charResult = METAPHONE_0;
				else
					throw new InvalidProgramException($"Unknown char was passed - {currentChar}!");

				result <<= 4;
				result |= charResult;
			};
			return result;
		}
	}
}
