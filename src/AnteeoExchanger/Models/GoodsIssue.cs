using System;

namespace AnteeoExchanger.Models
{
    public class GoodsIssue
    {
        public int trnid { get; set; }
        public int typ_dokumentu { get; set; }
        public string numer_pelny { get; set; }
        public string symbol { get; set; }
        public string data_dokumentu { get; set; }
        public string data_wystawienia { get; set; }
        public string data_operacji { get; set; }
        public int oper_status { get; set; }
        public DateTime data_utworzenia_rekordu { get; set; }
    }
}
