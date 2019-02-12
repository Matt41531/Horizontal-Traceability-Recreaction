/*
Tokenization
Author: Thanh Ngoc Dao - Thanh.dao@gmx.net
Copyright (c) 2005 by Thanh Ngoc Dao.
*/

using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;


namespace FeatureTool
{
	/// <summary>
	/// Summary description for Tokeniser.
	/// Partition string into SUBwords
	/// </summary>
	internal class Tokeniser
	{
        //PH 11 apr
        bool bStem; //true if stemming is done
        bool bRemoveStop; //true if stop words are removed
        bool bLower; //true if lower casing is done
        //System.IO.StreamWriter sw = new System.IO.StreamWriter("Removed.txt", true);

		public static string[] ArrayListToArray(ArrayList arraylist)
		{
			string[] array=new string[arraylist.Count] ;
			for(int i=0; i < arraylist.Count ; i++) array[i]=(string) arraylist[i];
			return array;
		}

		public string[] Partition(string input, bool bBi, bool bSyn, bool bCode, out int nrMatches)
		{
            //PH 5 apr
            //Regex r=new Regex("([ \\t{}():;. \n])");		
	        //PH 15 apr
            //Regex r = new Regex(@"[a-z]");			

            if(bLower) input = input.ToLower();

            nrMatches = 0;

            if (bCode)
            {
                //remove cvs logs
                Regex r14 = new Regex(@"[C|c]hecking\sin(.|\n)*done");
                nrMatches += r14.Matches(input).Count;
                input = r14.Replace(input, "\n");
                Regex r15 = new Regex(@"[C|c]hecking\sin(.|\n)*revision.*\n");
                nrMatches += r15.Matches(input).Count;
                input = r15.Replace(input, "\n");
                Regex r16 = new Regex(@"/cvs/(.|\n)*revision.*\n"); 
                nrMatches += r16.Matches(input).Count;
                //Regex r16 = new Regex(@"/cvs");
                //test
                MatchCollection testMatches = Regex.Matches(input, r16.ToString());
                //foreach (Match m in testMatches)
                //{
                //    Debug.WriteLine(m.ToString());
                //}
                //do it
                input = r16.Replace(input, "\n");
            }

            //PH 15 apr
            // Remove URLs 
            Regex r1 = new Regex(@"\w+\://\S+");
            nrMatches += r1.Matches(input).Count;
            //test
            //MatchCollection matches1 = Regex.Matches(input, r1.ToString());
            //foreach (Match m in matches1)
            //{
            //   sw.WriteLine(m.ToString());
            //}
            //do it
            input = r1.Replace(input, " ");

            // Remove file paths
            Regex r2 = new Regex(@"((\w+/\w+(/\w+)+)|(\w+(\\\w+)+))");
            nrMatches += r2.Matches(input).Count;
            //test
            //MatchCollection matches2 = Regex.Matches(input, r2.ToString());
            //foreach (Match m in matches2)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r2.Replace(input, " ");

            // Remove quoted text
            Regex r3 = new Regex(@"\n>.*\n");
            nrMatches += r3.Matches(input).Count;
            //test
            //MatchCollection matches3 = Regex.Matches(input, r3.ToString());
            //foreach (Match m in matches3)
            //{
            //    sw.WriteLine(m.ToString());
            //}
            //sw.Close();
            //do it
            input = r3.Replace(input, "\n ");

            if (bCode)
            {
                //remove source code
                //remove stack traces
                Regex r4 = new Regex(@"\n\s*at\s*\S+(\.\S)*\(.*\)");
                nrMatches += r4.Matches(input).Count;
                input = r4.Replace(input, "\n ");
                Regex r5 = new Regex(@"\n[\w\.]+[e|E]xception.*(?=\n)");
                nrMatches += r5.Matches(input).Count;
                input = r5.Replace(input, "\n ");
                Regex r6 = new Regex(@"\-\-\s[E|e]rror\s[D|d]etails(.|\n)*[E|e]xception\s[S|s]tack\s[t|T]race\:");
                nrMatches += r6.Matches(input).Count;
                input = r6.Replace(input, "\n ");
                
                //remove build ID and other related comments
                Regex r7 = new Regex(@"[B|b]uild\s[I|i][d|D].*\n");
                nrMatches += r7.Matches(input).Count;
                input = r7.Replace(input, "\n ");
                Regex r8 = new Regex(@"[U|u]ser\-[A|a]gent.*\n");
                nrMatches += r8.Matches(input).Count;
                input = r8.Replace(input, "\n ");
                Regex r9 = new Regex(@"[S|s]teps.*[R|r]eproduce\:");
                nrMatches += r9.Matches(input).Count;
                input = r9.Replace(input, " ");
                Regex r10 = new Regex(@"[M|m]ore\s[I|i]nformation\:");
                nrMatches += r10.Matches(input).Count;
                input = r10.Replace(input, " ");
                Regex r11 = new Regex(@"[R|r]eproducible\:.*(?=\n)");
                nrMatches += r11.Matches(input).Count;
                input = r11.Replace(input, " ");
                Regex r13 = new Regex(@"\n[T|t]hread\s\[.*\n(.*line.*\n)+");
                nrMatches += r13.Matches(input).Count;
                input = r13.Replace(input, "\n ");

                //remove code lines
                Regex r12 = new Regex(@"\n.*[\;|\}|\{](?=\n)");
                nrMatches += r12.Matches(input).Count;
                input = r12.Replace(input, "\n ");

                
            } //end if bCode

            // Get words
            MatchCollection matches = Regex.Matches(input, @"\b[a-z|A-Z]\w*((\.|\-|_|')*(\w+))*");
            String [] tokens = new String[matches.Count];
            int j = 0;
            foreach (Match m in matches)
            {
                tokens[j] = m.Value;
                j++;
            }
           
			ArrayList filter=new ArrayList() ;
            string strLast = "";

            //added stemming PH 27 march
            PorterStemming ps = new PorterStemming();

            for (int i = 0; i < tokens.Length; i++)
            {
                string s;
                if (bSyn)
                {
                    s = CorrectTerm(tokens[i]);
                }
                else
                {
                    s = tokens[i];
                }
                if (bRemoveStop)
                {
                    if (!StopWordsHandler.IsStopword(tokens[i]))
                    {
                        if (bStem)
                        {
                            s = ps.stemTerm(s.Replace("'", ""));
                        }
                        else
                        {
                            s = s.Replace("'", "");
                        }
                        if (!bBi) filter.Add(s);
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = s;
                    }
                    else
                    {
                        if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                        strLast = "";
                    }
                }
                else
                {
                    if (bStem)
                    {
                        s = ps.stemTerm(s.Replace("'", ""));
                    }
                    else
                    {
                        s = s.Replace("'", "");
                    }
                    if (!bBi) filter.Add(s);
                    if (strLast != "" && bBi) filter.Add(strLast + " " + s);
                    strLast = s;
                }
            }
			return ArrayListToArray (filter);
		}


		public Tokeniser(bool bStem, bool bRemoveStop, bool bLower)
		{
            this.bStem = bStem;
            this.bRemoveStop = bRemoveStop;
            this.bLower = bLower;
            lstWrong.Clear();
            lstWrong.AddRange(wrongWords);
		}

        public static string[] wrongWords = new string[] {  
            "ablity",
            "accidently",
            "achive",
            "appered",
            "assuption",
            "attachements",
            "auth",
            "avaliability",
            "behaviour",
            "bolded",
            "bugzill",
            "challening",
            "ci",
            "ctlr",
            "currenlty",
            "custonm",
            "defautl",
            "defs",
            "delgators",
            "depdencies",
            "dev",
            "diabled",
            "dialogue",
            "didnt",
            "diff",
            "dnd",
            "drop-down",
            "e.g",
            "elses",
            "e-mails",
            "enterting",
            "exisiting",
            "focussed",
            "impl",
            "incase",
            "int",
            "isse",
            "junit3",
            "junit4",
            "key-bindings",
            "maintainance",
            "meta-data",
            "peformquery",
            "plug-in",
            "presync",
            "read-only",
            "recognise",
            "reopen",
            "repos",
            "reuse",
            "rev",
            "sec",
            "short-cut",
            "similiar",
            "simliar",
            "someting",
            "specifiy",
            "sub-menu",
            "submited",
            "submition",
            "sub-section",
            "sub-task",
            "sub-tasks",
            "successfull",
            "sufficent",
            "swe",
            "sync",
            "synchronisation",
            "synchronising",
            "synchronization",
            "syncing",
            "synctaskjob",
            "taks",
            "taskes",
            "task-repositories",
            "timeout",
            "validatesetttings",
            "wil",
            "work-arounds",
            "wouldt",
            "xml_rpc",
            "xmlrpc", 
            "unsubmitted",
            "dup"
        };

        public static string[] correctWords = new string[] { 
            "ability",
            "accidentally",
            "achieve",
            "appeared",
            "assumption",
            "attachments",
            "authentication",
            "availability",
            "behavior",
            "bold",
            "bugzilla",
            "challenging",
            "checkin",
            "ctrl",
            "currently",
            "custom",
            "default",
            "definitions",
            "delegators",
            "dependencies",
            "development",
            "disabled",
            "dialog",
            "didn't",
            "difference",
            "drag and drop",
            "dropdown",
            "abbr",
            "else's",
            "email",
            "entering",
            "existing",
            "focused",
            "implementation",
            "in case",
            "integer",
            "issue",
            "junit",
            "junit",
            "keybindings",
            "maintenance",
            "metadata",
            "performquery",
            "plugin",
            "presynchronization",
            "readonly",
            "recognize",
            "re-open",
            "repositories",
            "re-use",
            "revision",
            "seconds",
            "shortcut",
            "similar",
            "similar",
            "something",
            "specify",
            "submenu",
            "submitted",
            "submission",
            "subsection",
            "subtask",
            "subtasks",
            "successful",
            "sufficient",
            "we",
            "synchronization",
            "synchronization",
            "synchronizing",
            "synchronization",
            "synchronizing",
            "synchronizetasksjob",
            "task",
            "tasks",
            "taskrepositories",
            "time-out",
            "validatesettings",
            "will",
            "workarounds",
            "would",
            "xml-rpc",
            "xml-rpc",
            "submit",
            "duplicate"
        };

        List<string> lstWrong = new List<string>();

        private string CorrectTerm(string input)
        {
            if (lstWrong.Contains(input))
            {
                return correctWords[lstWrong.IndexOf(input)];
            }
            else
            {
                return input;
            }
        }
       
	}
}
