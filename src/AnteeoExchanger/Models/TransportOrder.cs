namespace AnteeoExchanger.Models
{
    public class TransportOrder
    {
        public string Generator { get; set; }
        public int Typ_Dokumentu { get; set; }
        public string Numer_Pelny { get; set; }
        public string Data_Dokumentu { get; set; }
        public string Data_Wystawienia { get; set; }
        public string Data_Operacji { get; set; }
        public string Opis { get; set; }
        public string Uwagi_Magazyn { get; set; }
        public string Osoba_Kontakt { get; set; }
        public string Osoba_Telefon { get; set; }
        public string Przewoznik { get; set; }
        public string Kod { get; set; }
        public string NIP { get; set; }
        public string Nazwa { get; set; }
        public string Kod_Pocztowy { get; set; }
        public string Miasto { get; set; }
        public string Ulica { get; set; }
        public string Nr_Dom { get; set; }
        public string Kraj { get; set; }
        public string Nazwa_Dostawa { get; set; }
        public string Kod_Pocztowy_Dostawa { get; set; }
        public string Miasto_Dostawa { get; set; }
        public string Ulica_Dostawa { get; set; }
        public string Nr_Dom_Dostawa { get; set; }
        public string Kraj_Dostawa { get; set; }
    }
}
