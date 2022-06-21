using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 改写自
namespace CEmoji
{
	public class EmojiParser
	{
		/**
         *
         * \p{Emoji}
         *
         * zwj_element := \p{Emoji} emoji_modification?
         *
         *
         * Emoji的码点。这个方法不一定靠谱，通过unicode v12规范 emoji表观察归纳所得。
         * 随着unicode版本更新，这个codePoint的范围可能会增大。
         *
         * Basic Emoji's code point. This method is not necessarily reliable.
         * It is observed and summarized by the full Emoji table of the Unicode V12 specification.
         *
         * see [full-emoji-list](https://unicode.org/emoji/charts-12.0/full-emoji-list.html).
         */
		public static bool IsEmojiCodePoint(int codePoint)
		{
			// utf32的判断
			if( (codePoint>=0x1F200 && codePoint<=0x1FFFF) || 
				(codePoint>=0x2500 && codePoint<=0x2FFF) || IsSpecialSymbol(codePoint))
			{
				return true;
			}
			return false;
		}

		//0xE000-0xE02F 用作自定义 Emoji, 其余 Private Use Area 区间用做所有 emoji 的唯一编号 
		public static bool IsCustomEmojiPoint(int codePoint) 
        {
            if (codePoint>= 0xE000 && codePoint <= 0xE02F)
            {
				return true;
            }

			return false;
        }

		// 单个char（utf16）的判断
		public static bool IsEmojiSingleChar(int codePoint)
		{
			if((codePoint>=0xD800 && 0xD800<=0xDFFF) || 
				(codePoint>=0x2500 && codePoint<=0x2FFF) || IsSpecialSymbol(codePoint))
			{
				return true;
			}
			return false;
		}
		public static bool IsSpecialSymbol(int codePoint)
		{
			if(
				codePoint == 0x3030 || //wavy dash
                codePoint == 0x00A9 || //copyright
                codePoint == 0x00AE || //registered
                codePoint == 0x2122 || //trade mark
				codePoint == 0x303D || //part alternation mark
				codePoint == 0x3297 || //circled ideograph congratulation
				codePoint == 0x3299 //circled ideograph secret
			)
			{
				return true;
			}
			return false;
		}

		private const int MIN_SUPPLEMENTARY_CODE_POINT = 0x010000;
		
		/**
		* possible_emoji :=  zwj_element (\x{200D} zwj_element)+
		*
		* 200D 连接两个emoji元素成为一个新的emoji
		*/
		public readonly int Joiner = 0x200D;
		
		/**
		* emoji_modification :=
		* \p{EMD}
		* | (\x{FE0F} | \p{Me}) \p{Me}*
		* | tag_modifier
		*
		* E.g 0x23 0xFE0F 0x20E3
		*/
		//emoji_presentation 
		// 例如 U+26A1 U+FE0F 则指定高电压符号号使用 emoji 风格渲染，而 U+26A1 U+FE0E 则指定高电压符号使用 text 风格渲染
		public static int ModifierBlack = 0xFE0E;	
		public static int ModifierColorFul = 0xFE0F;
		// KeyCap Sequence。这类 emoji 序列是将数字（0-9），* 与 # 通过一个 U+20E3 字符转换为键帽的样式
		public static int ModifierKeyCap = 0x20E3;

		// Tag Sequence。Tag Sequence 由 tag_base tag_spec 与 tag_term 拼接而成的 emoji 序列串。其中 tag_base 可以是基本 emoji 字符，也可以是emoji_modifier_sequence 或者 emoji_presentation_sequence。 tag_spec 允许是从 U+E0020 到 U+E007E 的所有字符，而 tag_term 为U+E007F 作为结束符。目前只有三个合法 tag sequence，这种方式主要用于未来扩展使用。
		public static int ModifierTagRange_0 = 0xE0020;
		public static int ModifierTagRange_1 = 0xE007F;

		//Modfier_Base_Sequence 。这种是一种Modifier_Base + Modifer 修饰的方式。其中 Modifier 是用于对 Modifier_Base 进行修饰的 unicode 字符，目前定义了五种修饰字符，分别表示颜色的由深及浅，1F3FB到1F3FF 
		public static int ModifierSkinTone_0 = 0x1F3FB;
		public static int ModifierSkinTone_1 = 0x1F3FC;
		public static int ModifierSkinTone_2 = 0x1F3FD;
		public static int ModifierSkinTone_3 = 0x1F3FE;
		public static int ModifierSkinTone_4 = 0x1F3FF;
		
		public static ObjectPool<CharacterNode> s_nodePool = new ObjectPool<CharacterNode>(null, OnReleaseCharacterNode);
		private readonly List<int> emojiModifiers = new List<int>{
			ModifierKeyCap,
			ModifierBlack,
			ModifierColorFul,
			ModifierSkinTone_0,
			ModifierSkinTone_1,
			ModifierSkinTone_2,
			ModifierSkinTone_3,
			ModifierSkinTone_4,
		};

		//状态机的状态
		enum EmojiState
		{
			STATE_DEFAULT = 0x0,
			STATE_EMOJI = 0x1,
			STATE_PRE_EMOJI = 0x10,
			STATE_NATIONAL_FLAG = 0x101,	// 0x1&0x101
			STATE_EMOJI_MODIFIER = 0x1001,	// 0x1&0x1000
			STATE_EMOJI_JOIN = 0x10000,
		}

		private List<CharacterNode> charUnitList = new List<CharacterNode>();
		private int currentIndex = 0;
		private int currentCodePoint = 0;		// 代理对会转换成utf32
		private int currentCodeLength = 0;		// 有代理对的code长度是2，没有的是1
		private CharacterNode currentChar;
		private EmojiState currentState = EmojiState.STATE_DEFAULT;
		

		public int GetCurrentCharSize()
		{
			return charUnitList.Count;
		}

		public List<CharacterNode> GetCharacterList()
		{
			return charUnitList;
		}

		public  void Reset()
		{
			foreach(CharacterNode node in charUnitList)
			{
				s_nodePool.Release(node, false);
			}
			charUnitList.Clear();
			currentIndex = 0;
			currentChar = s_nodePool.Get();
			currentState = EmojiState.STATE_DEFAULT;
		}


		public void Parse(string str, int end=-1)
		{
			if(end == -1)
			{
				end = str.Length;
			}
			Reset();

			while(currentIndex < str.Length)
			{
				currentCodePoint = char.ConvertToUtf32(str, currentIndex);
				currentCodeLength = GetCharLength(currentCodePoint);
				switch(currentState)
				{
					case EmojiState.STATE_EMOJI_JOIN:
					{
						if(IsEmojiCodePoint(currentCodePoint))
						{
							/**
                             * emoji + emoji
                             * +号后面是emoji，符合期望
                             */
							currentState = EmojiState.STATE_EMOJI;
							MoveToNext();
						}
						else
						{
							/**
                             * emoji + !emoji
                             * 因为 + 后面没有跟另一个emoji，所以回塑到 + 这个字符
                             * + 不再代表emoji的连=连接符
                             */
							 MoveToPrev();
							 EndChar();
						}
						break;
					}
					case EmojiState.STATE_NATIONAL_FLAG:
					{
						if(IsRegionIndicator(currentCodePoint))
						{
							/**
                             * flag_sequence := \p{RI} \p{RI}
                             *
                             * 两个国家区域，完成一个国旗emoji，符合期望
                             */
							 MoveToNext();
							 AssertEmoji();
							 EndChar();
						}
						else
						{
							/**
                             * 没达到两个国家区域，但前面的也是emoji
                             *
                             * 结束前面一个国家区域字符，并且在下一次遍历处理当前字符
                             */
							 AssertEmoji();
							 EndChar();
						}
						break;
					}
					case EmojiState.STATE_PRE_EMOJI:
					{
						if(IsEmojiModifier(currentCodePoint))
						{
							/**
                             * maybeEmoji Modifier*
                             *
                             * emoji 后面可以跟多个 Modifier
                             */
							currentState = EmojiState.STATE_EMOJI_MODIFIER;
							MoveToNext();
						}
						else
						{
							/**
                             * 结束前面一个字符，并且在下一次遍历处理当前字符
                             */
							EndChar();
						}
						break;
					}
					/**
                     * 当前是 Emoji 状态或者 Modifier 状态，
                     * 因为在 EBNF 里面 Emoji 和 Modifier 后面可以跟的码点是一样的，所以放在一起处理
                     */
					case EmojiState.STATE_EMOJI:
					case EmojiState.STATE_EMOJI_MODIFIER:
					{
						if(currentCodePoint == Joiner)
						{
							/**
                             * emoji + emoji
                             *
                             * 准备连接下一个emoji
                             */
							 currentState = EmojiState.STATE_EMOJI_JOIN;
							 MoveToNext();
						}
						else if(IsEmojiModifier(currentCodePoint))
						{
							/**
                             * emoji Modifier*
                             *
                             * emoji 或 Modifier 后面可以跟多个 Modifier
                             */
							 currentState = EmojiState.STATE_EMOJI_MODIFIER;
							 MoveToNext();
						}
						else
						{
							/**
                             * 结束前面一个Emoji，并且在下一次遍历处理当前字符
                             */
							 AssertEmoji();
							 EndChar();
						}
						break;
					}
					default:
					{
						 if(IsRegionIndicator(currentCodePoint))
						 {
							 /**
							* flag_sequence := \p{RI} \p{RI}
							*
							* 遇到第一个国家区域，等待下一个国家区域可以合并成一个国旗emoji
							*/
							 currentState = EmojiState.STATE_NATIONAL_FLAG;
							 MoveToNext();
						 }
						 else if(MaybeEmojiCodePoint(currentCodePoint))
						 {
							 /**
							* 有可能是emoji码点，由下一个码点是否是修饰符来决定
							*/
							currentState = EmojiState.STATE_PRE_EMOJI;
							MoveToNext();
						 }
						 else if(IsEmojiCodePoint(currentCodePoint) || IsCustomEmojiPoint(currentCodePoint))
						 {
							 /**
							* emoji码点，等待下一个 Join 或者 Modifier
							*/
							currentState = EmojiState.STATE_EMOJI;
							MoveToNext();
						 }
						 else
						 {
							 //普通字符
							 if(currentCodeLength>1)
							 {
								 Debug.LogErrorFormat("非emoji字符，但是字符大小超过一个char,codepoint:{0}",currentCodePoint);
							 }
							 MoveToNext();
							 EndChar();
						 }
						break;
					}
					
				}
				if(GetCurrentCharSize() >= end)
				{
					break;
				}
			}
			if(currentState != EmojiState.STATE_DEFAULT)
			{
				if(((int)currentState&(int)EmojiState.STATE_EMOJI) != 0)
				{
					AssertEmoji();
				}
				EndChar();
			}
		}

		private void EndChar()
		{
			currentState = EmojiState.STATE_DEFAULT;
			if(currentChar.codePoint.Count > 0)
			{
				charUnitList.Add(currentChar);
				currentChar = s_nodePool.Get();
				currentChar.startIndex = currentIndex;
			}
		}

		private void AssertEmoji()
		{
			currentChar.isEmoji = true;
		}

		private void MoveToNext()
		{
			currentChar.codePoint.Add(currentCodePoint);
			currentIndex += currentCodeLength;
			currentChar.charLength += currentCodeLength;
		}

		private void MoveToPrev()
		{
			List<int> codePoints = currentChar.codePoint;
			int lastIdx = codePoints.Count-1;
			int lastCodePoint = codePoints[lastIdx];
			codePoints.RemoveAt(lastIdx);
			int len = GetCharLength(lastCodePoint);
			currentIndex -= len;
			currentChar.charLength -= len;
		}

		// 获取
		private int GetCharLength(int codePoint)
		{
			return codePoint>=MIN_SUPPLEMENTARY_CODE_POINT?2:1;
		}
	

		/**
         * 不是独立emoji 要和修饰符一起用
         * 主要是方框数字，CodePoint = \p{Number} ModifierKeyCap
         * 样例： 0x39 0x20E3
         *
         * Not a stand-alone Emoji which should be used with modifiers,
         * CodePoint = \p{Number} ModifierKeyCap
         * E.g 0x39 0x20E3
         */
		 private bool MaybeEmojiCodePoint(int codePoint)
		 {
			 return (codePoint>=0x0030&&codePoint<=0x0039) || 
			 		(codePoint>=0x2B00&&codePoint<=0x2BFF) || 
					(codePoint>=0x2900&&codePoint<=0x297F) ||codePoint==0x0023;
		 }

		 /**
         * Regional_Indicator
         *
         * emoji_flag_sequence := regional_indicator regional_indicator
         *
         * 国家区域标识，两个标识会变成一个国旗emoji。
         * National regional indicator, two indicator will become a national flag Emoji.
         */
		 private bool IsRegionIndicator(int codePoint)
		 {
			 return codePoint>=0x1F000 && codePoint<=0x1F1FF;
		 }

		 private bool IsEmojiModifier(int codePoint)
		 {
			 if(codePoint>=ModifierTagRange_0 && codePoint<=ModifierTagRange_1)
			 {
				 return true;
			 }

			if(emojiModifiers.Contains(codePoint))
			{
				return true;
			}

			 return false;
		 }

		 private static void OnReleaseCharacterNode(CharacterNode node)
		 {
			 node.Clear();
		 }
	}

	public class CharacterNode
	{
		public int startIndex = 0;
		public bool isEmoji = false;
		public List<int> codePoint = new List<int>();
		public int charLength = 0;	// 字符长度（一个char代表1）
		public void Clear()
		{
			startIndex = 0;
			isEmoji = false;
			codePoint.Clear();
			charLength = 0;
		}
	}
}

