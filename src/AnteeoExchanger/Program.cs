using System;
using System.Collections.Generic;
using System.Linq;

namespace AnteeoExchanger
{
    class Program
    {
        static void Main(string[] args)
        {
            var dictionary = args.Select(a => a.Split('='))
                                 .ToDictionary(a => a[0], a => a.Length == 2 ? a[1] : null);

            Console.WriteLine("---------- Przygotowanie synchronizacji danych ----------");
            CommandHandle(dictionary);
        }

        public static void CommandHandle(Dictionary<string, string> dictionary)
        {
            var client = new AnteeoClient();

            if (dictionary.ContainsKey("typ-awiza"))
            {
                var type = dictionary["typ-awiza"];

                if (type == "pz")
                {
                    Console.WriteLine("---------- Awizacja dokumentu PZ ----------");
                    client.GoodsNoteAdvice(Convert.ToInt32(dictionary["rok"]), Convert.ToInt32(dictionary["rodzaj"]), Convert.ToInt32(dictionary["nr"]));
                    return;
                }

                if (type == "wz")
                {
                    Console.WriteLine("---------- Awizacja dokumentu WZ ----------");
                    client.DeliveryNoteAdvice(Convert.ToInt32(dictionary["nr"]));
                    return;
                }

                if (type == "zwz")
                {
                    Console.WriteLine("---------- Awizacja dokumentu ZWZ ----------");
                    client.ReturnGoodsNoteAdvice(Convert.ToInt32(dictionary["nr"]));
                    return;
                }
            }

            if (dictionary.ContainsKey("status"))
            {
                var statusCheck = dictionary["status"];
                if (statusCheck == "on")
                {
                    Console.WriteLine("---------- Sprawdzenie statusu wysłanych awizacji ----------");
                    client.GetGoodsIssueAdviceStatus();
                    client.SendDeliveryAdviceNumber();
                    client.GetDeliveryAdviceStatus();
                    client.GetReturnGoodsIssueAdviceStatus();
                    return;
                }
            }

            if (dictionary.ContainsKey("faktura"))
            {
                var sendInvoice = dictionary["faktura"];
                if (sendInvoice == "tak")
                {
                    Console.WriteLine("---------- Wysyłka faktury do awizacji WZ ----------");
                    client.AddDocuments(Convert.ToInt32(dictionary["nr"]), 0, 2);
                    return;
                }
            }

            if (dictionary.ContainsKey("jakosc"))
            {
                var sendQualityDocuments = dictionary["jakosc"];
                if (sendQualityDocuments == "tak")
                {
                    Console.WriteLine("---------- Wysyłka dokumentów jakościowych do awizacji WZ ----------");
                    client.AddDocuments(Convert.ToInt32(dictionary["nr"]), 0, 1);
                    return;
                }
            }
        }
    }
}