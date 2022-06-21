using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CEmoji;

// namespace UnityEngine.UI
// {
    public partial class TextEx : Text
    {
        #region Emoji参数
        [SerializeField]
        public bool m_supportEmoji;
        
        private EmojiMap m_emojiMap = null;
        private bool m_bNeedTranslateEmoji = true;

        #endregion


        TextGenerator m_temporaryTextGenerator;
        TextGenerator TemporaryTextGenerator { get { return m_temporaryTextGenerator ?? (m_temporaryTextGenerator = new TextGenerator()); } }

        public override string text
        {
            get
            {
                return base.text;
            }
            set
            {
                string tmp = value ?? "";
                string str = tmp;

                //emoji
                if(m_supportEmoji)
                {
                    if(m_emojiMap == null)
                    {
                        m_emojiMap = new EmojiMap();
                    }
                    if(m_bNeedTranslateEmoji)
                    {
                        str = m_emojiMap.Translate(str);
                    }
                    
                    str = EmojiMap.ReplaceEmojiToSpace(str);
             
                }

                base.text = str;
            }
        }

    #region Emoji相关处理

    static Dictionary<string, Sprite> EmojiSpriteDic = new Dictionary<string, Sprite>();

    
    public static void AddEmoji(string emj, Sprite sprite)
    {
        string strEmojiKey = GetConvertedString(emj);
        EmojiSpriteDic[strEmojiKey] = sprite;
        EmojiMap.AddEmojiKey(strEmojiKey);
    }
    
    public static void ClearEmojiDic()
    {
        EmojiSpriteDic.Clear();
        EmojiMap.ClearEmojiKey();
    }

    // 提供给InputField使用，InputField内部会做emoji解析，因此直接将结果传过来
    
    public void SetEmojiText(string strTranslatedText, string strReplaced)
    {
        if(m_emojiMap == null)
        {
            m_emojiMap = new EmojiMap();
        }
        m_emojiMap.SetTranslatedText(strTranslatedText);
        m_bNeedTranslateEmoji = false;
        text = strReplaced;

        m_bNeedTranslateEmoji = true;
    }

    static string GetConvertedString(string inputString)
    {
        string[] converted = inputString.Split('-');
        for (int j = 0; j < converted.Length; j++)
        {
            // fromBase value 中数字的基数，它必须是 2、8、10 或 16(进制)
            converted[j] = char.ConvertFromUtf32(Convert.ToInt32(converted[j], 16));
        }
        return string.Join(string.Empty, converted);
    }
    

    #endregion

        //文本行数，返回值未必正确，调用preferredHeight后可获得正确值（其他未测试，目前使用前会主动调用preferredHeight，因此该处不重复计算）
        public int lineCount
        {
            get
            {
                return cachedTextGeneratorForLayout.lineCount;
            }
        }
        
        readonly UIVertex[] m_TempVerts = new UIVertex[4];
        
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            if (font == null)
                return;

            // We don't care if we the font Texture changes while we are doing our Update.
            // The end result of cachedTextGenerator will be valid for this instance.
            // Otherwise we can get issues like Case 619238.
            m_DisableFontTextureRebuiltCallback = true;

            Vector2 extents = rectTransform.rect.size;

            var settings = GetGenerationSettings(extents);
            cachedTextGenerator.PopulateWithErrors(text, settings, gameObject);

            // Apply the offset to the vertices
            IList<UIVertex> verts = cachedTextGenerator.verts;
            float unitsPerPixel = 1 / pixelsPerUnit;
            int vertCount = verts.Count;

            // We have no verts to process just return (case 1037923)
            if (vertCount <= 0)
            {
                toFill.Clear();
                return;
            }

            // emoji和隔壁字符留一点空隙 不要太挤
            int emjOffset = 1;

            Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
            roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
            toFill.Clear();
            if (roundingOffset != Vector2.zero)
            {
                for (int i = 0; i < vertCount; ++i)
                {
                    int tempVertsIndex = i & 3;
                    m_TempVerts[tempVertsIndex] = verts[i];
                    m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
                    m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
                    m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
                    if (tempVertsIndex == 3)
                        toFill.AddUIVertexQuad(m_TempVerts);
                }
            }
            else
            {
                for (int i = 0; i < vertCount; i += 4)
                {
                    int characterIndex = i / 4;
                    Vector2[] uv1 = null;
                    
                    if(m_supportEmoji && m_emojiMap!=null && characterIndex<text.Length && text[characterIndex]==EmojiMap.EMJSPACE)
                    {
                        string strKey = m_emojiMap.GetEmojiKeyOfIndex(characterIndex);
                        Sprite sprite;
                        if(!string.IsNullOrEmpty(strKey) && EmojiSpriteDic.TryGetValue(strKey, out sprite)&&sprite!=null)
                        {
                            uv1 = sprite.uv;
                        }
                    }
                    
                    
                    if (uv1 != null)
                    {
                        m_TempVerts[0] = verts[i + 0];
                        m_TempVerts[1] = verts[i + 1];
                        m_TempVerts[2] = verts[i + 2];
                        m_TempVerts[3] = verts[i + 3];

                        m_TempVerts[0].position *= unitsPerPixel;
                        m_TempVerts[1].position *= unitsPerPixel;
                        m_TempVerts[2].position *= unitsPerPixel;
                        m_TempVerts[3].position *= unitsPerPixel;

                        m_TempVerts[0].position += new Vector3(2+emjOffset, fontSize-4-emjOffset, 0);
                        m_TempVerts[1].position += new Vector3(fontSize-emjOffset, fontSize-4-emjOffset, 0);
                        m_TempVerts[2].position += new Vector3(fontSize-emjOffset, -2+emjOffset, 0);
                        m_TempVerts[3].position += new Vector3(2+emjOffset, -2+emjOffset, 0);

                        m_TempVerts[0].uv1 = uv1[0];
                        m_TempVerts[1].uv1 = uv1[1];
                        m_TempVerts[2].uv1 = uv1[3];
                        m_TempVerts[3].uv1 = uv1[2];
                    }
                    else
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            m_TempVerts[j] = verts[i + j];
                            m_TempVerts[j].position *= unitsPerPixel;
                            m_TempVerts[j].uv1 = Vector2.zero;
                        }
                    }
                    toFill.AddUIVertexQuad(m_TempVerts);
                }
            }

            m_DisableFontTextureRebuiltCallback = false;
        }

    }
// }

