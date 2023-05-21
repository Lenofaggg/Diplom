using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VDS.Common;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query.Inference;

namespace DTopology
{
    /// <summary>
    /// Диалектическое дерево, индуцирует Д-топологию.
    /// </summary>
    public class DTree : Graph
    {
        // Поле содержит словарь пар: ключ, данные,
        // обеспечивает быструю выборку данных по ключу.
        // Ключ - строковое имя понятия, без префикса - двоеточия.
        // Данные - объект класса Notion.
        // Класс Notion содержит поля параметров понятия. В данной реализации это имя понятия - противоположности, 
        // sn - уровень в ДД. Корень имеет уровень 1, его непосредственные потомки уровень 2 и т.д.
        // Словарь понятий должен содержать все понятия, таким образом, пары противоположностей представлены дважды,
        // как данные о каждой из противоположностей.

        IDictionary<string, Notion> notion = new Dictionary<string, Notion>();

        /// <summary>
        /// Загрузка словаря понятий из файла.<br/>
        /// Возвращает null в случае успешного занесения данных в словарь.<br/> 
        /// Если понятие уже содержится в словаре, возвращает имя понятия и прекращает ввод данных.<br/>
        /// </summary>
        public string? LoadNotionsDictionary(string path)
        {
            string? s;
            string notion_name;
            notion.Clear();
            using (StreamReader sr = new StreamReader(path))
            {
                while ((s = sr.ReadLine()) != null)
                {
                    // что-нибудь делаем с прочитанной строкой s
                    Notion nt = new Notion();
                    string[] param = s.Split('\t');
                    notion_name = param[0];
                    nt.opposite = param[1];
                    nt.sn = long.Parse(param[2]);

                    // Занесение данных понятия notion_name в словарь.
                    if (notion.TryAdd<string, Notion>(notion_name, nt) == false)
                        // Выбросить исключение: понятие с именем notion_name уже есть в словаре.
                        return notion_name;
                }
                return null;
            }
        }

        /// <summary>
        /// Сохранение словаря понятий в файле.
        /// </summary>
        public void SaveNotionsDictionary(string path)
        {
            using (StreamWriter file = new StreamWriter(path))
                foreach (var entry in notion)
                {
                    Notion nt = entry.Value;
                    string? opposite = nt.opposite;
                    long sn = nt.sn;

                    file.WriteLine("{0}\t{1}\t{2}", entry.Key, opposite, sn);
                }
        }

        public void SaveResultSemanticSearch(string path, string[][] selection)
        {
            using (StreamWriter file = new StreamWriter(path))
                foreach (var item in selection)
                {

                    file.WriteLine(string.Join(", ", item));
                }
        }

        /// <summary>
        /// Проверка того, что все противоположности понятий словаря содержатся в словаре.<br/>
        /// В случае успеха возвращает null.<br/>
        /// Если противоположность не содержится в словаре, возвращается ее имя.<br/>
        /// </summary>
        public string? All_opposites_is_present()
        {
            foreach (var entry in notion)
            {
                Notion nt = entry.Value;
                Notion? value;
                string? opposite = nt.opposite;
                if (notion.TryGetValue(opposite, out value) == false)
                    return opposite;

            }
            return null;
        }

        /// <summary>
        /// Возвращает понятие, противоположность данному name.
        /// </summary>
        public string? Opposite(string name)
        {
            Notion? nt;
            if (notion.TryGetValue(name, out nt) == false)
                return "";
            return nt.opposite;
        }

        /// <summary>
        /// Возвращает семантический номер понятия name.<br/>
        /// Корень дерева имеет sn = 1.<br/>
        /// sn непосредственных потомков корня = 2 и т.д по иерархии.<br/>
        /// </summary>
        public long Sn(string name)
        {
            Notion? nt;
            if (notion.TryGetValue(name, out nt) == false)
                return -1;
            return nt.sn;
        }

        /// <summary>
        /// Возвращает строковое имя понятия, отбрасывая префикс url.<br/>
        /// Выполняет перекодирование кириллицы из escape последовательностей в UTF8.<br/>
        /// </summary>
        public string? DName(string? uriname)
        {
            if (uriname == null) return null;
            uriname = Uri.UnescapeDataString(uriname);
            int index_prefix_end = uriname.LastIndexOf('/') + 1;
            if (index_prefix_end == -1) return null;
            return uriname.Substring(index_prefix_end, uriname.Length - index_prefix_end);
        }

        /// <summary>
        /// Возвращает строковое имя понятия, отбрасывая префикс url.<br/>
        /// Выполняет перекодирование кириллицы из escape последовательностей в UTF8.<br/>
        /// </summary>
        public string? DName(INode? uri)
        {
            if (uri == null) return null;
            return DName(uri.ToString());
        }
        /// <summary>
        /// Возвращает субъект д-определения понятия - name.<br/>
        /// Возвращает "" если name корень дерева.<br/>
        /// Возвращает null если name = null.<br/>
        /// </summary>
        public string? Def_subject(string? name)
        {
            if (name == null) return null;
            if (name == "одно") return "";
            IUriNode uri = this.CreateUriNode(":" + name);
            IEnumerable<Triple> t = this.GetTriplesWithObject(uri);
            if (t.Count() > 1) return null; // Выбросить исключение - некорректное дерево.

            return DName(t.ElementAt<Triple>(0).Subject);
        }

        /// <summary>
        /// true, если d потомок (descendant) a предок (ancestor). false иначе.
        /// </summary>
        public bool DA(string? d, string? a)
        {
            if (d == null || a == null) return false;
            if (d == "" || a == "") return false;
            if (d == a) return true;
            if (Sn(a) > Sn(d)) return false;

            return DA(Def_subject(d), a);
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ближайший общий предок 2х понятий
        /// </summary>
        /// <param name="a">первое понятие</param>
        /// <param name="b">второе понятие</param>
        /// <returns>общий предок 2х понятий</returns>
        public string? NCA(string? a, string? b)
        {
            if (a == null || b == null || a == "" || b == "") return null;
            //if (a == "nil" || b == "nil") return "nil"; //определить nil
            if (a == b) return a;
            if (Sn(a) == 1 && Sn(b) == 1) return a;
            if (Sn(a) >= Sn(b)) return NCA(Def_subject(a), b);

            return NCA(a, Def_subject(b));
        }

        /// <summary>
        /// Выбор из пары понятий ближайшего к третьему
        /// </summary>
        /// <param name="nt0"></param>
        /// <param name="nt1"></param>
        /// <param name="nt"></param>
        /// <returns>Возвращает 0, если понятия сравнимы и nt0 ближе к nt, чем nt1.
        /// Возвращает 1, если понятия сравнимы и nt1 ближе к nt, чем nt0.
        /// Возвращает 2, если понятия сравнимы и nt0 и nt1 одинаково близки к nt.</returns>
        public int? NearestNotion(string? nt0, string? nt1, string? nt)
        {
            if (nt0 == null || nt1 == null || nt == null) return null;
            if (nt0 == "" || nt1 == "" || nt == "") return null;
            //if (nt0 == "nil" || nt1 == "nil" || nt == "nil") return "nil"; //определить nil

            if (Sn(NCA(nt0, nt)) > Sn(NCA(nt1, nt))) return 0;
            if (Sn(NCA(nt0, nt)) < Sn(NCA(nt1, nt))) return 1;
            if (NCA(nt0, nt) == NCA(nt1, nt)) return 2;
            return 3;
        }
        /// <summary>
        /// Выбор из пары векторов понятий ближайшего к третьему
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public int? NearestVec(string[]? v0, string[]? v1, string[]? v)
        {
            // Векторы не сравнимы
            if (!(v0.Length == v1.Length && v0.Length == v.Length)) return null;
            if (v0 == null || v1 == null || v == null) return null;
            // Векторы сравнимы
            for (int i = 0; i < v.Length; i++)
            {
                //if (v0[i] =="nil" || v1[i] == "nil" || v[i] == "nil") continue; 
                if (NearestNotion(v0[i], v1[i], v[i]) == 2) continue;
                return NearestNotion(v0[i], v1[i], v[i]);
            }
            return 2;
        }

        /// <summary>
        /// Выбор из массива векторов понятий ближайшего к вектору v
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public int? NearestArrVec(string[][]? arr, string[]? v)
        {
            if (arr == null || v == null || arr.Length == 0) return null;
            int nearest = 0;
            for (int i = 1; i < arr.Length; i++)
            {
                if (NearestVec(arr[nearest], arr[i], v) == null) return null;
                if (NearestVec(arr[nearest], arr[i], v) == 0) return 0;
                if (NearestVec(arr[nearest], arr[i], v) == 2) continue;
                if (NearestVec(arr[nearest], arr[i], v) == 1) nearest = i;
            }
            return nearest;
        }

        /// <summary>
        /// Сортировка массива векторов понятий по близости к вектору v
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public string[][]? SortVec(string[][]? arr, string[]? v)
        {
            if (arr == null || v == null || arr.Length == 0) return null;
            string[][]? sortArr = (string[][]?)arr.Clone();
            List<string[]> copyArr = new List<string[]>(arr);
            int? current = 0;
            // Текущий размер массива – результата сортировки
            for (int i = 0; i < arr.Length; i++)
            {
                int? nearest = NearestArrVec(copyArr.ToArray(), v);
                if (nearest == null) return null;
                else
                {
                    sortArr[(int)current++] = copyArr[(int)nearest];
                    copyArr.RemoveAt((int)nearest);
                }
            }
            return sortArr;
        }
        public Cluster[] ClusterVec(string[][] arrVec, string[][] means)
        {
            if (arrVec == null || means == null || arrVec.Length == 0 || means.Length == 0)
            {
                return null;
            }
            Cluster[] clusters = new Cluster[means.Length];
            for (int i = 0; i < clusters.Length; i++)
            {
                clusters[i] = new Cluster();
            }
            for (int i = 0; i < arrVec.Length; i++)
            {
                int? nCluster = NearestArrVec(means, arrVec[i]);
                if (nCluster == null || nCluster == -1)
                {
                    return null;
                }
                clusters[nCluster.Value].Add(arrVec[i]);
            }
            return clusters;
        }

        public Dictionary<string, Cluster> ClusterAnalysis(Dictionary<string, string[]> vectors, string[][] means)
        {
            // Проверка входных данных на наличие векторов и средних значений
            if (vectors == null || means == null || vectors.Count == 0 || means.Length == 0)
            {
                return null;
            }

            // Преобразование словаря векторов в двумерный массив
            string[][] arrVec = vectors.Values.ToArray();

            // Создание кластеров с помощью метода ClusterVec
            Cluster[] clustersArray = ClusterVec(arrVec, means);
            if (clustersArray == null)
            {
                return null;
            }

            // Преобразование массива кластеров в словарь с использованием ключей из входного словаря векторов
            Dictionary<string, Cluster> clusters = new Dictionary<string, Cluster>();
            for (int i = 0; i < clustersArray.Length; i++)
            {
                clusters[vectors.Keys.ElementAt(i)] = clustersArray[i];
            }

            return clusters;
        }
    }

    public class Notion
    {
        public string? opposite { get; set; }

        public long sn { get; set; }
    }
}