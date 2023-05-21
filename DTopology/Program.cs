using Lucene.Net.Index;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using VDS.Common;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Inference;
using VDS.RDF.Writing;
//using System.Text;
//using System.Text.Unicode;
using VDS.RDF.Writing.Formatting;
using static Lucene.Net.Util.Packed.PackedInt32s;
using static System.Net.Mime.MediaTypeNames;
//using static System.Net.Mime.MediaTypeNames;

namespace DTopology
{
    class Program
    {
        static void Main()
        {
            TestDriver test = new TestDriver();
            int faults = 0;
            int tests = 0;
            //current_directory = Directory.GetCurrentDirectory();

            // Файл - отчет о тестировании.
            // Содержит имена групп тестов и номера тестов в пределах группы,
            // закончившихся неудачей.
            FileInfo f = new FileInfo("../../../System/testResults.txt");
            StreamWriter testResults = f.CreateText();

            //test.Test_Common(testResults, ref tests, ref faults);
            test.Test_DTree(testResults, ref tests, ref faults);
            
            test.TestResults(tests, faults);
            testResults.Close();

        }
    }
}