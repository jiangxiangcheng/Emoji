using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

namespace CEmoji
{
    public class EmojiMap:EmojiParser
    {
        private const int EMOJI_SEQ_START = 0xE030; //0xE000-0xE02F 用作自定义 Emoji
        public static char EMJSPACE = '\u2001';
        private static string DEFAULT_EMOJI = "\u2753";
        private static int s_emojiSeq = EMOJI_SEQ_START;  //使用E000-F8FF之间的编码表示emoji
        static Dictionary<string, int> s_emoji2code = new Dictionary<string, int>();
        static Dictionary<int,string> s_code2emoji = new Dictionary<int, string>();
        
        public static void AddEmojiKey(string emojiKey)
        {
            if(s_emoji2code.ContainsKey(emojiKey))
            {
                Debug.LogErrorFormat("EmojiMap AddEmojiCode,重复，key:{0}", emojiKey);
            }
            s_emoji2code[emojiKey] = s_emojiSeq;
            s_code2emoji[s_emojiSeq] = emojiKey;
            ++s_emojiSeq;
        }

        
        public static void ClearEmojiKey()
        {
            s_emojiSeq = EMOJI_SEQ_START;
            s_emoji2code.Clear();
            s_code2emoji.Clear();
        }

        public static string ReplaceEmojiToSpace(string strText)
        {
            if(string.IsNullOrEmpty(strText))
            {
                return strText;
            }
            return Regex.Replace(strText, @"[\uE000-\uF8FF]", EMJSPACE.ToString());
        }

        private string m_strTransText;      // emoji转换成privateCode之后的串

        public static string GetEmojiKeyOfCode(int code, out bool hit)
        {
            hit = false;
            string emoji;
            if (s_code2emoji.TryGetValue(code, out emoji))
            {
                hit = true;
                return emoji;
            }
            else
            {
                return DEFAULT_EMOJI;
            }
        }

        public string GetEmojiKeyOfIndex(int index)
        {
            int code = (int)m_strTransText[index];
            string strKey;
            s_code2emoji.TryGetValue(code, out strKey);
            return strKey;
        }

        public void SetTranslatedText(string strTranslated)
        {
            m_strTransText = strTranslated;
        }

        public static string InvTranslate(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }

            try
            {
                bool hit;
                var sb = StringBuilderPool.Get(str.Length * 2);
                string tmp = string.Empty;
                foreach (char c in str)
                {
                    tmp = GetEmojiKeyOfCode(c, out hit);
                    if (hit)
                    {
                        sb.Append(tmp);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                str = sb.ToString();
                StringBuilderPool.Release(sb);
            }
            catch (System.Exception ex)
            {
                Debug.LogErrorFormat("InvTranslate Emoji Exception,msg:{0},stack trace:{1}", ex.Message, ex.StackTrace);
            }

            return str;
        }

        public string Translate(string input)
        {
            try
            {
                // Debugger.LogWarning("Emoji  Translate '{0}'", input);
                Parse(input);
                List<CharacterNode> chars = GetCharacterList();
                for(int i=chars.Count-1; i>=0; i--)
                {
                    CharacterNode charNode = chars[i];
                    if(charNode.isEmoji)
                    {
                        string strEmoji = input.Substring(charNode.startIndex, charNode.charLength);
                        input = input.Remove(charNode.startIndex, charNode.charLength);
                        if(!s_emoji2code.ContainsKey(strEmoji))
                        {
                            strEmoji = DEFAULT_EMOJI;
                        }
                        int emojiSeq = s_emoji2code[strEmoji];
                        string newChar = char.ConvertFromUtf32(emojiSeq);
                        input = input.Insert(charNode.startIndex, newChar);
                        
                        // Debug.LogFormat("input len:{0}, emojiSeq:{1:X}, emoji len:{2}", input.Length, emojiSeq, strEmoji.Length);
                    }
                    else
                    {
                        // 占2个char的字符替换成空格 不做支持
                        if(charNode.charLength>1)
                        {
                            input = input.Remove(charNode.startIndex, charNode.charLength);
                            input = input.Insert(charNode.startIndex, " ");
                        }
                    }
                }

                // Debugger.LogWarning("Emoji  Translate end '{0}'", input);

            }
            catch(System.Exception ex)
            {
                Debug.LogErrorFormat("Translate Emoji Exception,msg:{0},stack trace:{1}", ex.Message, ex.StackTrace);
            }
            m_strTransText = input;
            return input;
        }//TransEmojiCodeOfStr

    }
}