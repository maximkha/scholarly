using System;
using System.Linq;
using System.Collections.Generic;

namespace scholarly
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            schCore.AnswerCollection answers = null;
            schCore.AnswerWith answerWith = schCore.AnswerWith.Term;
            schCore.IndexWith indexWith = schCore.IndexWith.Bing;
            string additionalQuery = null;
            int limit = 25;
            while (true)
            {
                schCore.Utils.tableWidth = Console.WindowWidth;
                Console.Write("?>");
                string q = Console.ReadLine();
                string lower = q.ToLower();

                //if (lower.Contains("!e"))
                //{

                //    int startOfExclude = lower.IndexOf("!e", StringComparison.CurrentCulture) + 2;
                //    //if (lower.Length < startOfExclude) throw new Exception();
                //    string afterExclude = lower.Substring(startOfExclude);
                //    Console.WriteLine("excluding");
                //    additionalQuery = afterExclude;
                //    q = q.Substring(0, startOfExclude);
                //    continue;
                //}
                if (q[0]=='!')
                {
                    //Console.WriteLine(lower);
                    if (lower == "!clear")
                    {
                        Console.Clear();
                        continue;
                    }
                    else if (lower == "!exit")
                    {
                        break;
                    }
                    else if (lower.Substring(0, 2) == "!s")
                    {
                        additionalQuery = lower.Substring(2);
                        Console.WriteLine("Set Search Query: '" + additionalQuery + "'");
                        continue;
                    }
                    else if (lower.Substring(0,2) == "!i")
                    {
                        if (answers == null)
                        {
                            Console.WriteLine("NULL");
                            continue;
                        }
                        int s = schCore.Utils.ToInt32(lower.Substring(2));
                        schCore.Answer answer = answers.select(s);
                        schCore.Utils.printAnswer(answer);
                        continue;
                    }
                    else if (lower.Substring(0, 4) == "!ans")
                    {
                        string p = lower.Substring(4);
                        if (p == "term") answerWith = schCore.AnswerWith.Term;
                        else if (p == "def") answerWith = schCore.AnswerWith.Definition;
                        else if (p == "low") answerWith = schCore.AnswerWith.Lowest;
                        else
                        {
                            Console.WriteLine("'"+p+"' is not a valid option.");
                            continue;
                        }
                        Console.WriteLine("OK");
                        continue;
                    }
                    else if (lower.Substring(0, 5) == "!list")
                    {
                        int oldWidth = schCore.Utils.tableWidth;
                        schCore.Utils.tableWidth = oldWidth - 1;
                        string p = lower.Substring(5);
                        if (p.Substring(0, 2) == "!i")
                        {
                            int s = schCore.Utils.ToInt32(p.Substring(2));
                            schCore.Answer answer = answers.select(s);
                            List<Tuple<string, string>> termsDefs = schCore.Utils.getDefinitions(schCore.Utils.GetDocument(new Uri(answer.url)));
                            schCore.Utils.PrintLine();
                            schCore.Utils.PrintRow("term", "def");
                            foreach (Tuple<string, string> termDef in termsDefs)
                            {
                                schCore.Utils.PrintRow(termDef.Item1, termDef.Item2);
                            }
                            schCore.Utils.PrintLine();
                            schCore.Utils.tableWidth = oldWidth;
                            continue;
                        }
                        else
                        {
                            //TODO: implement
                            Console.WriteLine("'" + p + "' is not a valid option.");
                            schCore.Utils.tableWidth = oldWidth;
                            continue;
                        }
                    }
                    else if (lower.Substring(0, 6) == "!index")
                    {
                        string p = lower.Substring(6);
                        if (p == "google") indexWith = schCore.IndexWith.Google;
                        else if (p == "bing") indexWith = schCore.IndexWith.Bing;
                        else
                        {
                            Console.WriteLine("'" + p + "' is not a valid option.");
                            continue;
                        }
                        Console.WriteLine("OK");
                        continue;
                    }
                    else if (lower.Substring(0, 6) == "!limit")
                    {
                        string p = lower.Substring(6);
                        int s = schCore.Utils.ToInt32(lower.Substring(6));
                        limit = s;
                        Console.WriteLine("OK");
                        continue;
                    }
                }
                //what is the cyclops name?
                schCore.Question question = null;
                if (additionalQuery == null) question = new schCore.Question(q);
                else question = new schCore.Question(q, additionalQuery);
                answers = question.Answer(answerWith, indexWith);
                if (answers == null)
                {
                    Console.WriteLine("NULL");
                    continue;
                }
                answers.printTable(limit);
                additionalQuery = null;
            }
        }
    }
}
