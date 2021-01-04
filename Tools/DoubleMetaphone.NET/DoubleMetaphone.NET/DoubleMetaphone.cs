using System;
using System.Text;

namespace jabarnes.Metaphone
{
	/// <summary>
	///		Implements the Double Metaphone phonetic matching algorithm published
	///     by Lawrence Phillips in June 2000 C/C++ Users Journal. 
	///     Ported to .NET Standard by Jeremy Barnes.  
	/// </summary> 
	public class DoubleMetaphone
	{
		public const int METAPHONE_KEY_LENGTH = 4;//The length of the metaphone keys produced.  4 is sweet spot

		private StringBuilder PrimaryKeyBuilder, AltKeyBuilder;

		//As the key is built, we need a mutable tracker for its length
		private int PrimaryKeyRunningLength, AlternateKeyRunningLength;

		private string OriginalWordCopy;
		private int WordLength, WordLastIndex;

		/// <summary>
		/// Constructs a new DoubleMetaphone object, and initializes it with the metaphone keys for a given word
		/// </summary>
		/// 
		/// <param name="word">Word with which to initialize the object.  Computes the metaphone keys of this word.</param>
		public DoubleMetaphone(string word)
		{
			//Leave room at the end for writing a bit beyond the length; keys are chopped at the end anyway
			PrimaryKeyBuilder = new StringBuilder(METAPHONE_KEY_LENGTH + 2);
			AltKeyBuilder = new StringBuilder(METAPHONE_KEY_LENGTH + 2);

			ComputeKeys(word);
		}

		/// <summary>
		/// The primary metaphone key for the current word
		/// </summary>
		public string PrimaryKey
		{
			get; internal set;
		}

		/// <summary>
		/// The alternate metaphone key for the current word, or null if the current word does not have an alternate key by Double Metaphone
		/// </summary>
		public string AlternateKey
		{
			get; internal set;
		}

		/// <summary>Original word for which the keys were computed</summary>
		public string OriginalWord
		{
			get; internal set;
		}

		/// <summary>
		/// Sets a new current word for the instance, computing the new word's metaphone keys 
		/// </summary>
		/// 
		/// <param name="word">New word to set to current word.  Discards previous metaphone keys, and computes new keys for this word</param>
		public void ComputeKeys(string word)
		{
			PrimaryKeyBuilder.Length = 0;
			AltKeyBuilder.Length = 0;

			PrimaryKey = "";
			AlternateKey = "";

			PrimaryKeyRunningLength = AlternateKeyRunningLength = 0;

			OriginalWord = word;

			//Copy word to an internal working buffer so it can be modified
			OriginalWordCopy = word;

			WordLength = OriginalWordCopy.Length;

			//Compute last valid index into word
			WordLastIndex = WordLength - 1;

			//Padd with four spaces, so word can be over-indexed without fear of exception
			OriginalWordCopy = string.Concat(OriginalWordCopy, "     ");

			//Convert to upper case, since metaphone is not case sensitive
			OriginalWordCopy = OriginalWordCopy.ToUpper();

			//Now build the keys
			BuildMetaphoneKeys();
		}

		/// <summary>
		/// Internal impl of double metaphone algorithm. Modified copy-paste of Phillips' original code, updated to modern C#
		/// </summary>
		private void BuildMetaphoneKeys()
		{
			int current = 0;
			if (WordLength < 1)
				return;

			//skip these when at start of word
			if (ContainsAny(0, 2, "GN", "KN", "PN", "WR", "PS"))
				current += 1;

			//Initial 'X' is pronounced 'Z' e.g. 'Xavier'
			if (OriginalWordCopy[0] == 'X')
			{
				AddMetaphoneCharacter("S"); //'Z' maps to 'S'
				current += 1;
			}

			///////////main loop//////////////////////////
			while ((PrimaryKeyRunningLength < METAPHONE_KEY_LENGTH) || (AlternateKeyRunningLength < METAPHONE_KEY_LENGTH))
			{
				if (current >= WordLength)
					break;

				switch (OriginalWordCopy[current])
				{
					case 'A':
					case 'E':
					case 'I':
					case 'O':
					case 'U':
					case 'Y':
						if (current == 0)
							//all init vowels now map to 'A'
							AddMetaphoneCharacter("A");
						current += 1;
						break;

					case 'B':

						//"-mb", e.g", "dumb", already skipped over...
						AddMetaphoneCharacter("P");

						if (OriginalWordCopy[current + 1] == 'B')
							current += 2;
						else
							current += 1;
						break;

					case 'Ç':
						AddMetaphoneCharacter("S");
						current += 1;
						break;

					case 'C':
						//various germanic
						if ((current > 1)
							&& !IsVowel(current - 2)
							&& ContainsAny((current - 1), 3, "ACH")
							&& ((OriginalWordCopy[current + 2] != 'I') && ((OriginalWordCopy[current + 2] != 'E')
																  || ContainsAny((current - 2), 6, "BACHER", "MACHER"))))
						{
							AddMetaphoneCharacter("K");
							current += 2;
							break;
						}

						//special case 'caesar'
						if ((current == 0) && ContainsAny(current, 6, "CAESAR"))
						{
							AddMetaphoneCharacter("S");
							current += 2;
							break;
						}

						//italian 'chianti'
						if (ContainsAny(current, 4, "CHIA"))
						{
							AddMetaphoneCharacter("K");
							current += 2;
							break;
						}

						if (ContainsAny(current, 2, "CH"))
						{
							//find 'michael'
							if ((current > 0) && ContainsAny(current, 4, "CHAE"))
							{
								AddMetaphoneCharacter("K", "X");
								current += 2;
								break;
							}

							//greek roots e.g. 'chemistry', 'chorus'
							if ((current == 0)
								&& (ContainsAny((current + 1), 5, "HARAC", "HARIS")
									 || ContainsAny((current + 1), 3, "HOR", "HYM", "HIA", "HEM"))
								&& !ContainsAny(0, 5, "CHORE"))
							{
								AddMetaphoneCharacter("K");
								current += 2;
								break;
							}

							//germanic, greek, or otherwise 'ch' for 'kh' sound
							if ((ContainsAny(0, 4, "VAN ", "VON ") || ContainsAny(0, 3, "SCH"))
								// 'architect but not 'arch', 'orchestra', 'orchid'
								|| ContainsAny((current - 2), 6, "ORCHES", "ARCHIT", "ORCHID")
								|| ContainsAny((current + 2), 1, "T", "S")
								|| ((ContainsAny((current - 1), 1, "A", "O", "U", "E") || (current == 0))
									//e.g., 'wachtler', 'wechsler', but not 'tichner'
									&& ContainsAny((current + 2), 1, "L", "R", "N", "M", "B", "H", "F", "V", "W", " ")))
							{
								AddMetaphoneCharacter("K");
							}
							else
							{
								if (current > 0)
								{
									if (ContainsAny(0, 2, "MC"))
										//e.g., "McHugh"
										AddMetaphoneCharacter("K");
									else
										AddMetaphoneCharacter("X", "K");
								}
								else
									AddMetaphoneCharacter("X");
							}
							current += 2;
							break;
						}
						//e.g, 'czerny'
						if (ContainsAny(current, 2, "CZ") && !ContainsAny((current - 2), 4, "WICZ"))
						{
							AddMetaphoneCharacter("S", "X");
							current += 2;
							break;
						}

						//e.g., 'focaccia'
						if (ContainsAny((current + 1), 3, "CIA"))
						{
							AddMetaphoneCharacter("X");
							current += 3;
							break;
						}

						//double 'C', but not if e.g. 'McClellan'
						if (ContainsAny(current, 2, "CC") && !((current == 1) && (OriginalWordCopy[0] == 'M')))
							//'bellocchio' but not 'bacchus'
							if (ContainsAny((current + 2), 1, "I", "E", "H") && !ContainsAny((current + 2), 2, "HU"))
							{
								//'accident', 'accede' 'succeed'
								if (((current == 1) && (OriginalWordCopy[current - 1] == 'A'))
									|| ContainsAny((current - 1), 5, "UCCEE", "UCCES"))
									AddMetaphoneCharacter("KS");
								//'bacci', 'bertucci', other italian
								else
									AddMetaphoneCharacter("X");
								current += 3;
								break;
							}
							else
							{//Pierce's rule
								AddMetaphoneCharacter("K");
								current += 2;
								break;
							}

						if (ContainsAny(current, 2, "CK", "CG", "CQ"))
						{
							AddMetaphoneCharacter("K");
							current += 2;
							break;
						}

						if (ContainsAny(current, 2, "CI", "CE", "CY"))
						{
							//italian vs. english
							if (ContainsAny(current, 3, "CIO", "CIE", "CIA"))
								AddMetaphoneCharacter("S", "X");
							else
								AddMetaphoneCharacter("S");
							current += 2;
							break;
						}

						//else
						AddMetaphoneCharacter("K");

						//name sent in 'mac caffrey', 'mac gregor
						if (ContainsAny((current + 1), 2, " C", " Q", " G"))
							current += 3;
						else
							if (ContainsAny((current + 1), 1, "C", "K", "Q")
								&& !ContainsAny((current + 1), 2, "CE", "CI"))
							current += 2;
						else
							current += 1;
						break;

					case 'D':
						if (ContainsAny(current, 2, "DG"))
							if (ContainsAny((current + 2), 1, "I", "E", "Y"))
							{
								//e.g. 'edge'
								AddMetaphoneCharacter("J");
								current += 3;
								break;
							}
							else
							{
								//e.g. 'edgar'
								AddMetaphoneCharacter("TK");
								current += 2;
								break;
							}

						if (ContainsAny(current, 2, "DT", "DD"))
						{
							AddMetaphoneCharacter("T");
							current += 2;
							break;
						}

						//else
						AddMetaphoneCharacter("T");
						current += 1;
						break;

					case 'F':
						if (OriginalWordCopy[current + 1] == 'F')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("F");
						break;

					case 'G':
						if (OriginalWordCopy[current + 1] == 'H')
						{
							if ((current > 0) && !IsVowel(current - 1))
							{
								AddMetaphoneCharacter("K");
								current += 2;
								break;
							}

							if (current < 3)
							{
								//'ghislane', ghiradelli
								if (current == 0)
								{
									if (OriginalWordCopy[current + 2] == 'I')
										AddMetaphoneCharacter("J");
									else
										AddMetaphoneCharacter("K");
									current += 2;
									break;
								}
							}
							//Parker's rule (with some further refinements) - e.g., 'hugh'
							if (((current > 1) && ContainsAny((current - 2), 1, "B", "H", "D"))
								//e.g., 'bough'
								|| ((current > 2) && ContainsAny((current - 3), 1, "B", "H", "D"))
								//e.g., 'broughton'
								|| ((current > 3) && ContainsAny((current - 4), 1, "B", "H")))
							{
								current += 2;
								break;
							}
							else
							{
								//e.g., 'laugh', 'McLaughlin', 'cough', 'gough', 'rough', 'tough'
								if ((current > 2)
									&& (OriginalWordCopy[current - 1] == 'U')
									&& ContainsAny((current - 3), 1, "C", "G", "L", "R", "T"))
								{
									AddMetaphoneCharacter("F");
								}
								else
									if ((current > 0) && OriginalWordCopy[current - 1] != 'I')
									AddMetaphoneCharacter("K");

								current += 2;
								break;
							}
						}

						if (OriginalWordCopy[current + 1] == 'N')
						{
							if ((current == 1) && IsVowel(0) && !IsWordSlavoGermanic())
							{
								AddMetaphoneCharacter("KN", "N");
							}
							else
								//not e.g. 'cagney'
								if (!ContainsAny((current + 2), 2, "EY")
									&& (OriginalWordCopy[current + 1] != 'Y') && !IsWordSlavoGermanic())
							{
								AddMetaphoneCharacter("N", "KN");
							}
							else
								AddMetaphoneCharacter("KN");
							current += 2;
							break;
						}

						//'tagliaro'
						if (ContainsAny((current + 1), 2, "LI") && !IsWordSlavoGermanic())
						{
							AddMetaphoneCharacter("KL", "L");
							current += 2;
							break;
						}

						//-ges-,-gep-,-gel-, -gie- at beginning
						if ((current == 0)
							&& ((OriginalWordCopy[current + 1] == 'Y')
								 || ContainsAny((current + 1), 2, "ES", "EP", "EB", "EL", "EY", "IB", "IL", "IN", "IE", "EI", "ER")))
						{
							AddMetaphoneCharacter("K", "J");
							current += 2;
							break;
						}

						// -ger-,  -gy-
						if ((ContainsAny((current + 1), 2, "ER") || (OriginalWordCopy[current + 1] == 'Y'))
							&& !ContainsAny(0, 6, "DANGER", "RANGER", "MANGER")
							&& !ContainsAny((current - 1), 1, "E", "I")
							&& !ContainsAny((current - 1), 3, "RGY", "OGY"))
						{
							AddMetaphoneCharacter("K", "J");
							current += 2;
							break;
						}

						// italian e.g, 'biaggi'
						if (ContainsAny((current + 1), 1, "E", "I", "Y") || ContainsAny((current - 1), 4, "AGGI", "OGGI"))
						{
							//obvious germanic
							if ((ContainsAny(0, 4, "VAN ", "VON ") || ContainsAny(0, 3, "SCH"))
								|| ContainsAny((current + 1), 2, "ET"))
								AddMetaphoneCharacter("K");
							else
								//always soft if french ending
								if (ContainsAny((current + 1), 4, "IER "))
								AddMetaphoneCharacter("J");
							else
								AddMetaphoneCharacter("J", "K");
							current += 2;
							break;
						}

						if (OriginalWordCopy[current + 1] == 'G')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("K");
						break;

					case 'H':
						//only keep if first & before vowel or btw. 2 vowels
						if (((current == 0) || IsVowel(current - 1))
							&& IsVowel(current + 1))
						{
							AddMetaphoneCharacter("H");
							current += 2;
						}
						else//also takes care of 'HH'
							current += 1;
						break;

					case 'J':
						//obvious spanish, 'jose', 'san jacinto'
						if (ContainsAny(current, 4, "JOSE") || ContainsAny(0, 4, "SAN "))
						{
							if (((current == 0) && (OriginalWordCopy[current + 4] == ' ')) || ContainsAny(0, 4, "SAN "))
								AddMetaphoneCharacter("H");
							else
							{
								AddMetaphoneCharacter("J", "H");
							}
							current += 1;
							break;
						}

						if ((current == 0) && !ContainsAny(current, 4, "JOSE"))
							AddMetaphoneCharacter("J", "A");//Yankelovich/Jankelowicz
						else
							//spanish pron. of e.g. 'bajador'
							if (IsVowel(current - 1)
								&& !IsWordSlavoGermanic()
								&& ((OriginalWordCopy[current + 1] == 'A') || (OriginalWordCopy[current + 1] == 'O')))
							AddMetaphoneCharacter("J", "H");
						else
							if (current == WordLastIndex)
							AddMetaphoneCharacter("J", " ");
						else
							if (!ContainsAny((current + 1), 1, "L", "T", "K", "S", "N", "M", "B", "Z")
								&& !ContainsAny((current - 1), 1, "S", "K", "L"))
							AddMetaphoneCharacter("J");

						if (OriginalWordCopy[current + 1] == 'J')//it could happen!
							current += 2;
						else
							current += 1;
						break;

					case 'K':
						if (OriginalWordCopy[current + 1] == 'K')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("K");
						break;

					case 'L':
						if (OriginalWordCopy[current + 1] == 'L')
						{
							//spanish e.g. 'cabrillo', 'gallegos'
							if (((current == (WordLength - 3))
								 && ContainsAny((current - 1), 4, "ILLO", "ILLA", "ALLE"))
								|| ((ContainsAny((WordLastIndex - 1), 2, "AS", "OS") || ContainsAny(WordLastIndex, 1, "A", "O"))
									&& ContainsAny((current - 1), 4, "ALLE")))
							{
								AddMetaphoneCharacter("L", " ");
								current += 2;
								break;
							}
							current += 2;
						}
						else
							current += 1;
						AddMetaphoneCharacter("L");
						break;

					case 'M':
						if ((ContainsAny((current - 1), 3, "UMB")
							 && (((current + 1) == WordLastIndex) || ContainsAny((current + 2), 2, "ER")))
							//'dumb','thumb'
							|| (OriginalWordCopy[current + 1] == 'M'))
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("M");
						break;

					case 'N':
						if (OriginalWordCopy[current + 1] == 'N')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("N");
						break;

					case 'Ñ':
						current += 1;
						AddMetaphoneCharacter("N");
						break;

					case 'P':
						if (OriginalWordCopy[current + 1] == 'H')
						{
							AddMetaphoneCharacter("F");
							current += 2;
							break;
						}

						//also account for "campbell", "raspberry"
						if (ContainsAny((current + 1), 1, "P", "B"))
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("P");
						break;

					case 'Q':
						if (OriginalWordCopy[current + 1] == 'Q')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("K");
						break;

					case 'R':
						//french e.g. 'rogier', but exclude 'hochmeier'
						if ((current == WordLastIndex)
							&& !IsWordSlavoGermanic()
							&& ContainsAny((current - 2), 2, "IE")
							&& !ContainsAny((current - 4), 2, "ME", "MA"))
							AddMetaphoneCharacter("", "R");
						else
							AddMetaphoneCharacter("R");

						if (OriginalWordCopy[current + 1] == 'R')
							current += 2;
						else
							current += 1;
						break;

					case 'S':
						//special cases 'island', 'isle', 'carlisle', 'carlysle'
						if (ContainsAny((current - 1), 3, "ISL", "YSL"))
						{
							current += 1;
							break;
						}

						//special case 'sugar-'
						if ((current == 0) && ContainsAny(current, 5, "SUGAR"))
						{
							AddMetaphoneCharacter("X", "S");
							current += 1;
							break;
						}

						if (ContainsAny(current, 2, "SH"))
						{
							//germanic
							if (ContainsAny((current + 1), 4, "HEIM", "HOEK", "HOLM", "HOLZ"))
								AddMetaphoneCharacter("S");
							else
								AddMetaphoneCharacter("X");
							current += 2;
							break;
						}

						//italian & armenian
						if (ContainsAny(current, 3, "SIO", "SIA") || ContainsAny(current, 4, "SIAN"))
						{
							if (!IsWordSlavoGermanic())
								AddMetaphoneCharacter("S", "X");
							else
								AddMetaphoneCharacter("S");
							current += 3;
							break;
						}

						//german & anglicisations, e.g. 'smith' match 'schmidt', 'snider' match 'schneider'
						//also, -sz- in slavic language altho in hungarian it is pronounced 's'
						if (((current == 0)
							 && ContainsAny((current + 1), 1, "M", "N", "L", "W"))
							|| ContainsAny((current + 1), 1, "Z"))
						{
							AddMetaphoneCharacter("S", "X");
							if (ContainsAny((current + 1), 1, "Z"))
								current += 2;
							else
								current += 1;
							break;
						}

						if (ContainsAny(current, 2, "SC"))
						{
							//Schlesinger's rule
							if (OriginalWordCopy[current + 2] == 'H')
								//dutch origin, e.g. 'school', 'schooner'
								if (ContainsAny((current + 3), 2, "OO", "ER", "EN", "UY", "ED", "EM"))
								{
									//'schermerhorn', 'schenker'
									if (ContainsAny((current + 3), 2, "ER", "EN"))
									{
										AddMetaphoneCharacter("X", "SK");
									}
									else
										AddMetaphoneCharacter("SK");
									current += 3;
									break;
								}
								else
								{
									if ((current == 0) && !IsVowel(3) && (OriginalWordCopy[3] != 'W'))
										AddMetaphoneCharacter("X", "S");
									else
										AddMetaphoneCharacter("X");
									current += 3;
									break;
								}

							if (ContainsAny((current + 2), 1, "I", "E", "Y"))
							{
								AddMetaphoneCharacter("S");
								current += 3;
								break;
							}
							//else
							AddMetaphoneCharacter("SK");
							current += 3;
							break;
						}

						//french e.g. 'resnais', 'artois'
						if ((current == WordLastIndex) && ContainsAny((current - 2), 2, "AI", "OI"))
							AddMetaphoneCharacter("", "S");
						else
							AddMetaphoneCharacter("S");

						if (ContainsAny((current + 1), 1, "S", "Z"))
							current += 2;
						else
							current += 1;
						break;

					case 'T':
						if (ContainsAny(current, 4, "TION"))
						{
							AddMetaphoneCharacter("X");
							current += 3;
							break;
						}

						if (ContainsAny(current, 3, "TIA", "TCH"))
						{
							AddMetaphoneCharacter("X");
							current += 3;
							break;
						}

						if (ContainsAny(current, 2, "TH")
							|| ContainsAny(current, 3, "TTH"))
						{
							//special case 'thomas', 'thames' or germanic
							if (ContainsAny((current + 2), 2, "OM", "AM")
								|| ContainsAny(0, 4, "VAN ", "VON ")
								|| ContainsAny(0, 3, "SCH"))
							{
								AddMetaphoneCharacter("T");
							}
							else
							{
								AddMetaphoneCharacter("0", "T");
							}
							current += 2;
							break;
						}

						if (ContainsAny((current + 1), 1, "T", "D"))
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("T");
						break;

					case 'V':
						if (OriginalWordCopy[current + 1] == 'V')
							current += 2;
						else
							current += 1;
						AddMetaphoneCharacter("F");
						break;

					case 'W':
						//can also be in middle of word
						if (ContainsAny(current, 2, "WR"))
						{
							AddMetaphoneCharacter("R");
							current += 2;
							break;
						}

						if ((current == 0)
							&& (IsVowel(current + 1) || ContainsAny(current, 2, "WH")))
						{
							//Wasserman should match Vasserman
							if (IsVowel(current + 1))
								AddMetaphoneCharacter("A", "F");
							else
								//need Uomo to match Womo
								AddMetaphoneCharacter("A");
						}

						//Arnow should match Arnoff
						if (((current == WordLastIndex) && IsVowel(current - 1))
							|| ContainsAny((current - 1), 5, "EWSKI", "EWSKY", "OWSKI", "OWSKY")
							|| ContainsAny(0, 3, "SCH"))
						{
							AddMetaphoneCharacter("", "F");
							current += 1;
							break;
						}

						//polish e.g. 'filipowicz'
						if (ContainsAny(current, 4, "WICZ", "WITZ"))
						{
							AddMetaphoneCharacter("TS", "FX");
							current += 4;
							break;
						}

						//else skip it
						current += 1;
						break;

					case 'X':
						//french e.g. breaux
						if (!((current == WordLastIndex)
							  && (ContainsAny((current - 3), 3, "IAU", "EAU")
								   || ContainsAny((current - 2), 2, "AU", "OU"))))
							AddMetaphoneCharacter("KS");

						if (ContainsAny((current + 1), 1, "C", "X"))
							current += 2;
						else
							current += 1;
						break;

					case 'Z':
						//chinese pinyin e.g. 'zhao'
						if (OriginalWordCopy[current + 1] == 'H')
						{
							AddMetaphoneCharacter("J");
							current += 2;
							break;
						}
						else
							if (ContainsAny((current + 1), 2, "ZO", "ZI", "ZA")
								|| (IsWordSlavoGermanic() && ((current > 0) && OriginalWordCopy[current - 1] != 'T')))
						{
							AddMetaphoneCharacter("S", "TS");
						}
						else
							AddMetaphoneCharacter("S");

						if (OriginalWordCopy[current + 1] == 'Z')
							current += 2;
						else
							current += 1;
						break;

					default:
						current += 1;
						break;
				}
			}

			//Finally, chop off the keys at the proscribed length
			if (PrimaryKeyRunningLength > METAPHONE_KEY_LENGTH)
			{
				PrimaryKeyBuilder.Length = METAPHONE_KEY_LENGTH;
			}

			if (AlternateKeyRunningLength > METAPHONE_KEY_LENGTH)
			{
				AltKeyBuilder.Length = METAPHONE_KEY_LENGTH;
			}

			PrimaryKey = PrimaryKeyBuilder.ToString();
			AlternateKey = AltKeyBuilder.Length == 0 ? null : AltKeyBuilder.ToString();
		}

		private bool IsWordSlavoGermanic()
		{
			return OriginalWordCopy.Contains("W") ||
				   OriginalWordCopy.Contains("K") ||
				   OriginalWordCopy.Contains("CZ") ||
				   OriginalWordCopy.Contains("WITZ");
		}

		/// <summary>
		/// Returns true if letter at given position in word is a Roman vowel
		/// </summary>
		/// <param name="pos"></param>
		/// <returns></returns>
		private bool IsVowel(int pos)
		{
			if ((pos < 0) || (pos >= WordLength))
				return false;

			char it = OriginalWordCopy[pos];

			return (it == 'E') || (it == 'A') || (it == 'I') || (it == 'O') || (it == 'U') || (it == 'Y');
		}

		/// <summary>
		/// Appends the given metaphone character to the primary and alternate keys
		/// </summary>
		/// <param name="primaryCharacter"></param>
		private void AddMetaphoneCharacter(string primaryCharacter)
		{
			AddMetaphoneCharacter(primaryCharacter, null);
		}
		///**
		// * 
		// * 
		// * @param primaryCharacter
		// *               
		// * @param alternateCharacter
		// *               
		// */
		/// <summary>
		/// Appends a metaphone character to the primary, and a possibly different alternate, metaphone keys for the word.
		/// </summary>
		/// <param name="primaryCharacter">Character to append to primary key, and, if no alternate char is present</param>
		/// <param name="alternateCharacter">Character to append to alternate key. If not provided, the primary character will be appended to the alternate key</param>
		private void AddMetaphoneCharacter(string primaryCharacter, string alternateCharacter)
		{
			//Is the primary character valid?
			if (primaryCharacter.Length > 0)
			{
				int idx = 0;
				while (idx < primaryCharacter.Length)
				{
					PrimaryKeyBuilder.Length++;
					PrimaryKeyBuilder[PrimaryKeyRunningLength++] = primaryCharacter[idx++];
				}
			}

			//Is the alternate character valid?
			if (alternateCharacter != null)
			{
				//Alternate character was provided.  If it is not zero-length, append it, else
				//append the primary string as long as it wasn't zero length and isn't a space character
				if (alternateCharacter.Length > 0)
				{
					if (alternateCharacter[0] != ' ')
					{
						int idx = 0;
						while (idx < alternateCharacter.Length)
						{
							AltKeyBuilder.Length++;
							AltKeyBuilder[AlternateKeyRunningLength++] = alternateCharacter[idx++];
						}
					}
				}
				else
				{
					//No, but if the primary character is valid, add that instead
					if (primaryCharacter.Length > 0 && (primaryCharacter[0] != ' '))
					{
						int idx = 0;
						while (idx < primaryCharacter.Length)
						{
							AltKeyBuilder.Length++;
							AltKeyBuilder[AlternateKeyRunningLength++] = primaryCharacter[idx++];
						}
					}
				}
			}
			else if (primaryCharacter.Length > 0)
			{
				//Else, no alternate character was passed, but a primary was, so append the primary character to the alternate key
				int idx = 0;
				while (idx < primaryCharacter.Length)
				{
					AltKeyBuilder.Length++;
					AltKeyBuilder[AlternateKeyRunningLength++] = primaryCharacter[idx++];
				}
			}
		}

		/// <summary>
		/// Tests if any of the strings passed as variable arguments are at the given start position and length within word
		/// </summary>
		/// <param name="start">Start position in the original word</param>
		/// <param name="length">Length starting at 0 to search in original word</param>
		/// <param name="searchVals">Values to find in original word</param>
		/// <returns></returns>
		private bool ContainsAny(int start, int length, params string[] searchVals)
		{
			if (start < 0)
			{
				//Sometimes, as a result of expressions like "current - 2" for start, 
				//start ends up negative.  Since no string can be present at a negative offset, this is always false
				return false;
			}

			string target = OriginalWordCopy.Substring(start, length);

			for (int idx = 0; idx < searchVals.Length; idx++)
			{
				if (searchVals[idx] == target)
				{
					return true;
				}
			}

			return false;
		}
	}
}

