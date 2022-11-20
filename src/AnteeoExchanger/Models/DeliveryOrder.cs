namespace AnteeoExchanger.Models
{
    public class DeliveryOrder
    {
        public string Generator { get; set; }
        public int TypDokumentu { get; set; }
        public string NumerPelny { get; set; }
        public string DataDokumentu { get; set; }
        public string DataWystawienia { get; set; }
        public string DataOperacji { get; set; }
        public string Opis { get; set; }
        public string Kod { get; set; }
        public string NIP { get; set; }
        public string Nazwa { get; set; }
        public string KodPocztowy { get; set; }
        public string Miasto { get; set; }
        public string Ulica { get; set; }
        public string NrDom { get; set; }
        public string Kraj { get; set; }
    }
}
