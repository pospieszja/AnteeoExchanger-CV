namespace AnteeoExchanger.Models
{
    public class DeliveryPosition
    {
        public int trnid { get; set; }
        public int lp { get; set; }
        public int trpid { get; set; }
        public int twrid { get; set; }
        public string kod { get; set; }
        public string jm { get; set; }
        public string partia { get; set; }
        public double ilosc { get; set; }
        public string opk_kod { get; set; }
        public int opk_liczba { get; set; }
        public double opk_ilosc { get; set; }
        public double opk_liczba_suma { get; set; }
        public string paleta { get; set; }
        public int liczba_palet { get; set; }
        public string termin_waznosci { get; set; }
        public string magazyn { get; set; }
    }
}
