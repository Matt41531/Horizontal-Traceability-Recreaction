/*
Copyright 2011, Andrew Polar

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

//Updated version, Feb. 23, 2012.
//ERRORS: -1 word is NULL; -2 wrong length; -3 unknown; -4 nDictionarySize is too short;

namespace FeatureTool
{
    [Serializable]
    class EverGrowingDictionary
    {
        const int m_nDictionarySize = 0xffffff;

        private int m_nWordCounter;
        private int m_index;

        private int[] m_oneByteLookup = new int[256];
        private int[] m_twoByteLookup = new int[0xffff + 1];
        private char[] m_dictionary = new char[m_nDictionarySize];
        private int[] m_lookup = new int[0xffffff + 1];
        private int[] m_self = new int[m_nDictionarySize];
        private string[] m_vocabulary = null;

        public EverGrowingDictionary()
        {
            for (int i = 0; i < 256; ++i)
            {
                m_oneByteLookup[i] = 0;
            }
            for (int i = 0; i < 0xffff + 1; ++i)
            {
                m_twoByteLookup[i] = 0;
            }
            for (int i = 0; i < 0xffffff + 1; ++i)
            {
                m_lookup[i] = 0;
            }
            for (int i = 0; i < m_nDictionarySize; ++i)
            {
                m_self[i] = 0;
            }

            m_nWordCounter = 0;
            m_dictionary[0] = ' ';
            m_index = 1;
        }

        private int addToDictionary(char[] word, int len, int pos)
        {
            ++m_nWordCounter;
            int old_index = m_index;
            for (int k = 3; k < len; ++k)
            {
                m_dictionary[m_index++] = word[k];
                if (m_index >= m_nDictionarySize)
                {
                    //dictionary is full, next step cause buffer overflow
                    return -4;
                }
            }
            m_dictionary[m_index] = ' ';
            m_self[m_index] = m_nWordCounter;
            ++m_index;
            if (m_index >= m_nDictionarySize)
            {
                //dictionary is full, next step cause buffer overflow
                return -4;
            }
            m_self[old_index] = m_lookup[pos];
            m_lookup[pos] = old_index;
            return m_nWordCounter - 1;
        }

        public int processWord(char[] word, int len)
        {
            len = word.Length;
            int pos = 0;
            int m = len;
            if (m > 3) m = 3;
            for (int i = 0; i < m; ++i)
            {
                pos <<= 8;
                pos |= word[i];
            }
            pos &= 0x00ffffff;

            int lPos = m_lookup[pos];
            if (lPos == 0)
            {
                return addToDictionary(word, len, pos);
            }

            while (true)
            {
                bool isOK = true;
                int n = lPos;
                for (int k = 3; k < len; ++k, ++n)
                {
                    if (m_dictionary[n] != word[k])
                    {
                        isOK = false;
                        break;
                    }
                }
                if (isOK && m_dictionary[n] == 0x20)
                {
                    return m_self[n] - 1;  //that means the word is already in the dictionary
                }
                lPos = m_self[lPos];
                if (lPos == 0)
                {
                    return addToDictionary(word, len, pos);
                }
            }
        }

        public int GetDictionarySize()
        {
            return m_index;
        }

        public int GetNumberOfWords()
        {
            return m_nWordCounter;
        }

        public int GetWordIndex(string strWord)
        {
            if (strWord == null) return -1;
            int len = strWord.Length;
            if (len > 255) return -2;
            if (len == 0) return -5;
            char[] word = strWord.ToCharArray();
            if (len > 3) return processWord(word, len);

            if (len == 3)
            {
                char[] wordA = new char[4];
                wordA[0] = word[0];
                wordA[1] = word[1];
                wordA[2] = word[2];
                wordA[3] = '_';
                return processWord(wordA, 4);
            }
            if (len == 2)
            {
                int nPos = word[0];
                nPos <<= 8;
                nPos |= word[1];
                if (m_twoByteLookup[nPos] == 0)
                {
                    ++m_nWordCounter;
                    m_twoByteLookup[nPos] = m_nWordCounter;
                    return m_nWordCounter - 1;
                }
                else
                {
                    return m_twoByteLookup[nPos] - 1;
                }
            }
            if (len == 1)
            {
                if (m_oneByteLookup[word[0]] == 0)
                {
                    ++m_nWordCounter;
                    m_oneByteLookup[word[0]] = m_nWordCounter;
                    return m_nWordCounter - 1;
                }
                else
                {
                    return m_oneByteLookup[word[0]] - 1;
                }
            }
            return -3;
        }

        public void MakeVocabulary()
        {
            if (m_nWordCounter == 0) return;
            m_vocabulary = new string[m_nWordCounter];
            for (int i = 0; i < m_nWordCounter; ++i)
            {
                m_vocabulary[i] = string.Empty;
            }

            //one byte lookup
            for (int i = 0; i < 256; ++i)
            {
                if (m_oneByteLookup[i] != 0)
                {
                    m_vocabulary[m_oneByteLookup[i] - 1] = ((char)i).ToString();
                }
            }

            //two bytes lookup
            char[] s = new char[2];
            for (int i = 0; i < 0xffff + 1; ++i)
            {
                if (m_twoByteLookup[i] != 0)
                {
                    s[0] = (char)(i >> 8);
                    s[1] = (char)(i & 0xff);
                    m_vocabulary[m_twoByteLookup[i] - 1] = new string(s);
                }
            }

            //other words
            char[] w = new char[256];
            for (int i = 0; i < 0xffffff + 1; ++i)
            {
                if (m_lookup[i] != 0)
                {
                    w[0] = (char)((i >> 16) & 0xff);
                    w[1] = (char)((i >> 8) & 0xff);
                    w[2] = (char)(i & 0xff);

                    int startindex = m_lookup[i];
                    while (true)
                    {
                        int index = startindex;
                        int cnt = 3;
                        while (m_dictionary[index] != ' ')
                        {
                            w[cnt] = m_dictionary[index];
                            ++cnt;
                            ++index;
                            if (index >= m_nDictionarySize) break;
                        }

                        if (index == startindex) //actually we should not get here
                        {
                            string s1 = new string(w, 0, cnt);
                            m_vocabulary[m_self[index + 1] - 1] = s1;
                            break;
                        }

                        if (w[cnt - 1] == '_') --cnt; //filter underscore symbol at the end, if exists
                        string s2 = new string(w, 0, cnt);
                        m_vocabulary[m_self[index] - 1] = s2;
                        startindex = m_self[startindex];
                        if (startindex == 0) break;
                    }
                }
            }
        }

        public void OutputVocabulary(string filename)
        {
            if (m_vocabulary == null) return;

            using (StreamWriter fileWordsStream = new StreamWriter(filename))
            {
                for (int i = 0; i < m_nWordCounter; ++i)
                {
                    if (m_vocabulary[i] == string.Empty)
                    {
                        Debug.WriteLine("Vocabulary is corrupted.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        fileWordsStream.WriteLine(i.ToString() + " " + m_vocabulary[i]);
                    }
                }
                fileWordsStream.Flush();
                fileWordsStream.Close();
            }
        }
    }
}
