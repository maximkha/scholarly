using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Xml;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Linq;
using System.Globalization;

namespace scholarly
{
    public static class schCore
    {
        public enum AnswerWith
        {
            Term,
            Definition,
            Lowest
        }

        public enum IndexWith
        {
            Google,
            Bing
        }

        public class AnswerCollection
        {
            int numQuizlet;
            int numOther;
            string questionText;

            List<Answer> answers;

            public AnswerCollection(string _questionText, int _numQuizlet, int _numOther, List<Answer> _answers)
            {
                numQuizlet = _numQuizlet;
                numOther = _numOther;
                questionText = _questionText;
                answers = _answers.OrderBy((x) => x.score).ToList();
            }

            public void printTable(int limit = -1)
            {
                int total = numOther + numQuizlet;
                List<Answer> limited = new List<Answer>();
                if (limit > 0) limited = answers.Take(limit).ToList();

                Utils.PrintLine();

                int oldWidth = Utils.tableWidth;
                int indxLength = Utils.tableWidth.ToString().Length;
                Utils.tableWidth = oldWidth - (indxLength + 1); //+1 for the bar(|) on the left side
                Console.Write("|"+Utils.AlignCentre("i", indxLength));
                Utils.PrintRow("score", "term", "def", "url");
                //Utils.PrintRow("score", "source", "term", "def", "url");
                int i = 0;
                foreach (Answer answer in limited)
                {
                    Console.Write("|" + Utils.AlignCentre(i.ToString(), indxLength));
                    Utils.PrintRow(answer.score.ToString(), answer.term, answer.definition, answer.url);
                    //Utils.PrintRow(answer.score.ToString(), answer.answerWith.ToString(), answer.term, answer.definition, answer.url);
                    i++;
                }

                Utils.tableWidth = oldWidth;
                Utils.PrintLine();
                Utils.tableWidth = oldWidth - 1;
                Utils.PrintRow("Data", "Useful URLS:"+ String.Format("Value: {0:P2}", (double)numQuizlet/(double)total) +"("+ numQuizlet + "/"+ total + ")");
                Utils.tableWidth = oldWidth;
            }

            public Answer select(int i)
            {
                if (i > answers.Count - 1) return new Answer();
                return answers[i];
            }
        }

        public struct Answer
        {
            public int score;
            public string url;
            public string term;
            public string definition;
            public AnswerWith answerWith;
        }

        public class Question
        {
            string text;
            string searchText = null;

            public Question(string _text)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                text = _text;
            }

            public Question(string _text, string _searchText)
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                searchText = _searchText;
                text = _text;
            }

            public AnswerCollection Answer(AnswerWith with, IndexWith index)
            {
                List<string> urls = new List<string>();
                if (index.Equals(IndexWith.Google))
                {
                    HtmlDocument document = null;
                    if (searchText == null) document = Utils.GetDocument(new Uri("https://www.google.com/search?q=" + HttpUtility.UrlEncode("inurl:quizlet ") + HttpUtility.UrlEncode(text)));
                    else document = Utils.GetDocument(new Uri("https://www.google.com/search?q=" + HttpUtility.UrlEncode("inurl:quizlet ") + HttpUtility.UrlEncode(text) + HttpUtility.UrlEncode(searchText)));
                    HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//div[@class=\"g\"]/div[1]/div[1]/div[1]/a");
                    if (nodes == null) return null;
                    foreach (HtmlNode row in nodes)
                    {
                        urls.Add(row.Attributes["href"].Value);
                    }
                }
                else if (index.Equals(IndexWith.Bing))
                {
                    HtmlDocument document = null;
                    if (searchText == null) document = Utils.GetDocument(new Uri("https://www.bing.com/search?q=" + HttpUtility.UrlEncode("site:quizlet.com ") + HttpUtility.UrlEncode(text)));
                    else document = Utils.GetDocument(new Uri("https://www.bing.com/search?q=" + HttpUtility.UrlEncode("site:quizlet.com ") + HttpUtility.UrlEncode(text) + HttpUtility.UrlEncode(searchText)));
                    HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//li[@class=\"b_algo\"]/h2[1]/a");
                    if (nodes == null) return null;
                    foreach (HtmlNode row in nodes)
                    {
                        urls.Add(row.Attributes["href"].Value);
                    }
                }

                int numQuizlet = 0;
                int numOther = 0;

                List<Answer> answers = new List<Answer>();
                foreach(string url in urls)
                {
                    HtmlDocument quizletDoc = Utils.GetDocument(new Uri(url));
                    if (!Utils.isQuizlet(quizletDoc))
                    {
                        numOther++;
                        continue;
                    }
                    numQuizlet++;
                    List<Tuple<string, string>> TermDefinition = Utils.getDefinitions(quizletDoc);
                    foreach(Tuple<string, string> termDef in TermDefinition)
                    {
                        Answer answer = new Answer();
                        //answer.score = Utils.LevenshteinDistance(text, termDef.Value);
                        answer.answerWith = with;
                        if (with.Equals(AnswerWith.Term)) answer.score = Utils.LevenshteinDistance(HttpUtility.HtmlDecode(text), HttpUtility.HtmlDecode(termDef.Item1));
                        else if (with.Equals(AnswerWith.Definition)) answer.score = Utils.LevenshteinDistance(HttpUtility.HtmlDecode(text), HttpUtility.HtmlDecode(termDef.Item2));
                        else if (with.Equals(AnswerWith.Lowest))
                        {
                            int term = Utils.LevenshteinDistance(HttpUtility.HtmlDecode(text), HttpUtility.HtmlDecode(termDef.Item1));
                            int def = Utils.LevenshteinDistance(HttpUtility.HtmlDecode(text), HttpUtility.HtmlDecode(termDef.Item2));
                            if (term > def)
                            {
                                answer.score = def;
                                answer.answerWith = AnswerWith.Definition;
                            }
                            else
                            {
                                answer.score = term;
                                answer.answerWith = AnswerWith.Term;
                            }
                        }
                        answer.term = HttpUtility.HtmlDecode(termDef.Item1);
                        answer.definition = HttpUtility.HtmlDecode(termDef.Item2);
                        answer.url = url;
                        answers.Add(answer);
                    }
                }
                //Console.WriteLine(Utils.listToString(urls));
                return new AnswerCollection(text, numQuizlet, numOther, answers);
            }
        }

        public static class Utils
        {
            static HttpClient httpClient = new HttpClient();

            public static void printAnswer(Answer answer)
            {
                Console.WriteLine("Score : " + answer.score);
                Console.WriteLine("Source: " + (answer.answerWith == AnswerWith.Definition ? "Definition" : "Term"));
                Console.WriteLine("Url   : " + answer.url);
                Console.WriteLine("Term  : " + answer.term);
                Console.WriteLine("Def   : " + answer.definition);
            }

            public static string listToString<T>(List<T> list)
            {
                string ret = "[";
                for (int i = 0; i < list.Count; i++)
                {
                    ret += list[i] + ",";
                }
                return ret.Substring(0, ret.Length - 1) + "]";
            }

            public static HtmlDocument GetDocument(Uri uri)
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_2) AppleWebKit/601.3.9 (KHTML, like Gecko) Version/9.0.2 Safari/601.3.9");
                HtmlDocument document = new HtmlDocument();
                HttpResponseMessage responseMessage = httpClient.GetAsync(uri).Result;
                string html = responseMessage.Content.ReadAsStringAsync().Result;
                document.LoadHtml(html);
                return document;
            }

            public static bool isQuizlet(HtmlDocument document)
            {
                HtmlNodeCollection nodes = document.DocumentNode.SelectNodes("//span[contains(@class,\"TermText\")]");
                return nodes == null || nodes.Count > 0;
            }

            public static List<Tuple<string, string>> getDefinitions(HtmlDocument document)
            {
                //HtmlNodeCollection htmlNodes = document.DocumentNode.SelectNodes("//span[contains(@class,\"TermText\")]");
                //List<Tuple<string, string>> termDefintion = new List<Tuple<string, string>>();
                //if (htmlNodes == null) return termDefintion;
                //string term = "";
                //for (int i = 0; i < htmlNodes.Count; i++)
                //{
                //    //HACK
                //    //TODO:FIX THIS

                //    if (i % 2 == 0)
                //    {
                //        term = htmlNodes[i].InnerText;
                //    }
                //    else
                //    {
                //        termDefintion.Add(new Tuple<string,string>(term, htmlNodes[i].InnerText));
                //    }
                //}
                //return termDefintion;

                List<Tuple<string, string>> termDefintion = new List<Tuple<string, string>>();
                HtmlNodeCollection answerNodes = document.DocumentNode.SelectNodes("//span[contains(@class,\"TermText\")]/parent::*[contains(@class,\"definition\")]");
                if (answerNodes == null) return termDefintion;
                HtmlNodeCollection questionNodes = document.DocumentNode.SelectNodes("//span[contains(@class,\"TermText\")]/parent::*[contains(@class,\"word\")]");
                if (questionNodes == null) return termDefintion;
                if (answerNodes.Count != questionNodes.Count)
                {
                    throw new Exception("Lengths don't match");
                }
                for (int i = 0; i < Math.Min(answerNodes.Count,questionNodes.Count); i++)
                {
                    termDefintion.Add(new Tuple<string, string>(questionNodes[i].InnerText, answerNodes[i].InnerText));
                }
                return termDefintion;
            }

            //https://rosettacode.org/wiki/Levenshtein_distance#C.23
            public static int LevenshteinDistance(string s, string t)
            {
                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                if (n == 0)
                {
                    return m;
                }

                if (m == 0)
                {
                    return n;
                }

                for (int i = 0; i <= n; i++)
                    d[i, 0] = i;
                for (int j = 0; j <= m; j++)
                    d[0, j] = j;

                for (int j = 1; j <= m; j++)
                    for (int i = 1; i <= n; i++)
                        if (s[i - 1] == t[j - 1])
                            d[i, j] = d[i - 1, j - 1];  //no operation
                        else
                            d[i, j] = Math.Min(Math.Min(
                                d[i - 1, j] + 1,    //a deletion
                                d[i, j - 1] + 1),   //an insertion
                                d[i - 1, j - 1] + 1 //a substitution
                                );
                return d[n, m];
            }

            //https://stackoverflow.com/questions/856845/how-to-best-way-to-draw-table-in-console-app-c
            public static int tableWidth = 150;

            public static void PrintLine()
            {
                Console.WriteLine(new string('-', tableWidth));
            }

            public static void PrintRow(params string[] columns)
            {
                int width = (tableWidth - columns.Length) / columns.Length;
                string row = "|";

                foreach (string column in columns)
                {
                    row += AlignCentre(column, width) + "|";
                }

                Console.WriteLine(row);
            }

            public static string AlignCentre(string text, int width)
            {
                text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

                if (string.IsNullOrEmpty(text))
                {
                    return new string(' ', width);
                }
                else
                {
                    return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
                }
            }

            //https://stackify.com/convert-csharp-string-int/
            public static int ToInt32(string value)
            {
                if (value == null)
                    return 0;
                return int.Parse(value, (IFormatProvider)CultureInfo.CurrentCulture);
            }
        }
    }
}
