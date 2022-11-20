using AnteeoExchanger.AnteeoService;
using AnteeoExchanger.DAL;
using AnteeoExchanger.Helpers;
using AnteeoExchanger.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AnteeoExchanger
{
    public class AnteeoClient
    {
        private AnteeoServicesXml_WMSClient _client = new AnteeoServicesXml_WMSClient();
        private AuthData _authDataObj = new AuthData();
        private GraffitiRepository _repository = new GraffitiRepository();

        public AnteeoClient()
        {
            _authDataObj.login = "LOGIT";
            _authDataObj.pass = "PASSWORD";
            _authDataObj.masterFid = "API_KEY";
        }

        #region Synchronizacja produktów

        public void ProductSynchronizeAll()
        {
            var executeDate = DateTime.Now;

            var products = _repository.GetAllProducts();

            if (products != null)
            {
                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("TOWARY",
                                from product in products
                                select new XElement("TOWAR",
                                    new XElement("KOD", product.Kod),
                                    new XElement("NAZWA", product.Nazwa),
                                    new XElement("ASORTYMENT", product.Asortyment),
                                    new XElement("JM", product.Jm),
                                    new XElement("EAN", product.EAN),
                                    new XElement("OPIS", product.Opis),
                                    new XElement("TOWAR_STATUS", 1)
                                )
                            )
                        )
                    );

                var result = _client.addXmlProduct(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var data = from item in xmlResult.Descendants("POZYCJA")
                            .Where(i => i.Element("STATUS").Value == "OK")
                           select new
                           {
                               kod = item.Element("KOD").Value,
                               twrid = Convert.ToInt32(item.Element("ID").Value)
                           };
                foreach (var item in data)
                {
                    _repository.UpdateProduct(item.kod, item.twrid);
                }

                SaveResult("productSynchronizeAll", xmlResult, executeDate);
            }
        }

        public void ProductModifySingle(string kod)
        {
            var executeDate = DateTime.Now;

            var products = _repository.GetAllProducts().Where(x => x.Kod == kod);

            if (products != null)
            {
                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("TOWARY",
                                from product in products
                                select new XElement("TOWAR",
                                    new XElement("ID", product.Id),
                                    new XElement("KOD", product.Kod),
                                    new XElement("NAZWA", product.Nazwa),
                                    new XElement("ASORTYMENT", product.Asortyment),
                                    new XElement("JM", product.Jm),
                                    new XElement("EAN", product.EAN),
                                    new XElement("OPIS", product.Opis)
                                )
                            )
                        )
                    );

                var result = _client.modifyXmlProduct(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                SaveResult("productModifySingle", xmlResult, executeDate);
            }
        }

        private void ProductSynchronizeSingle(string kod)
        {
            var executeDate = DateTime.Now;

            var product = _repository.GetSingleProduct(kod);

            if (product != null)
            {
                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("TOWARY",
                                new XElement("TOWAR",
                                    new XElement("KOD", product.Kod),
                                    new XElement("NAZWA", product.Nazwa),
                                    new XElement("ASORTYMENT", product.Asortyment),
                                    new XElement("JM", product.Jm),
                                    new XElement("EAN", product.EAN),
                                    new XElement("OPIS", product.Opis),
                                    new XElement("TOWAR_STATUS", 1)
                                )
                            )
                        )
                    );

                var result = _client.addXmlProduct(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var data = from item in xmlResult.Descendants("POZYCJA")
                            .Where(i => i.Element("STATUS").Value == "OK")
                           select new
                           {
                               kod = item.Element("KOD").Value,
                               twrid = Convert.ToInt32(item.Element("ID").Value)
                           };
                foreach (var item in data)
                {
                    _repository.UpdateProduct(item.kod, item.twrid);
                }

                SaveResult("productSynchronizeSingle", xmlResult, executeDate);
            }
        }

        #endregion

        #region Dokument PZ

        public void GoodsNoteAdvice(int rok, int rodzaj, int nr_dokumentu)
        {
            var executeDate = DateTime.Now;

            var deliveryOrder = _repository.GetDeliveryOrder(rok, rodzaj, nr_dokumentu);

            if (deliveryOrder != null)
            {
                XDocument request =
                    new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("GENERATOR", deliveryOrder.Generator),
                            new XElement("TYP_DOKUMENTU", deliveryOrder.TypDokumentu),
                            new XElement("NUMER_PELNY", deliveryOrder.NumerPelny),
                            new XElement("DATA_DOKUMENTU", deliveryOrder.DataDokumentu),
                            new XElement("DATA_WYSTAWIENIA", deliveryOrder.DataWystawienia),
                            new XElement("DATA_OPERACJI", deliveryOrder.DataOperacji),
                            new XElement("OPIS", deliveryOrder.Opis),
                            new XElement("KONTRAHENT",
                                new XElement("KOD", deliveryOrder.Kod),
                                new XElement("NIP", deliveryOrder.NIP),
                                new XElement("NAZWA", deliveryOrder.Nazwa),
                                new XElement("ADRES",
                                    new XElement("KOD_POCZTOWY", deliveryOrder.KodPocztowy),
                                    new XElement("MIASTO", deliveryOrder.Miasto),
                                    new XElement("ULICA", deliveryOrder.Ulica),
                                    new XElement("KRAJ", deliveryOrder.Kraj)
                                )
                            )
                        )
                    )
                );

                var result = _client.generateXmlAdvice(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var isCorrect = xmlResult.Descendants("ROOT").Any(i => i.Element("STATUS").Value == "OK");

                if (isCorrect)
                {
                    var advice = (from item in xmlResult.Descendants("ROOT")
                                  select new
                                  {
                                      Id = Convert.ToInt32(item.Element("ID").Value),
                                      Sygnatura = item.Element("SYGNATURA").Value
                                  }).SingleOrDefault();

                    GoodsDeliveryPositions(executeDate, rok, nr_dokumentu, advice.Id);
                    SetAdviceStatus(advice.Id);
                    _repository.SetExportStatusOnDeliveryNote(rok, nr_dokumentu, rodzaj, advice.Id);
                }

                SaveResult("goodsNoteAdviceHeader", xmlResult, executeDate);
            }
        }

        private void GoodsDeliveryPositions(DateTime executeDate, int rok, int nr_dokumentu, int trnid)
        {
            var deliveryOrderPositions = _repository.GetDeliveryOrderPosition(rok, nr_dokumentu);

            foreach (var item in deliveryOrderPositions)
            {
                ProductSynchronizeSingle(item.Kod);
            }

            deliveryOrderPositions = _repository.GetDeliveryOrderPosition(rok, nr_dokumentu);

            XDocument request =
                new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("TRNID", trnid)
                        ),
                        new XElement("POZYCJE",
                        from item in deliveryOrderPositions
                        select
                            new XElement("POZYCJA",
                                new XElement("LP", item.Lp),
                                new XElement("TWRID", item.Twrid),
                                new XElement("ILOSC", item.Ilosc),
                                new XElement("JM", item.Jm)
                            )
                        )
                    )
                );

            var result = _client.addXmlAdvicePositions(ConvertRequest(request.ToString()), _authDataObj);

            var xmlResult = CreateXML(result);

            SaveResult("goodsNoteAdvicePositions", xmlResult, executeDate);
        }

        private void CreateGoodsIssue(XDocument xmlResult)
        {
            var header = (from item in xmlResult.Descendants("NAGLOWEK").Skip(1)
                          select new GoodsIssue
                          {
                              trnid = Convert.ToInt32(item.Element("TRNID").Value),
                              typ_dokumentu = Convert.ToInt32(item.Element("TYP_DOKUMENTU").Value),
                              symbol = "PZ",
                              numer_pelny = item.Element("NUMER_PELNY").Value,
                              data_dokumentu = item.Element("DATA_DOKUMENTU").Value,
                              data_wystawienia = item.Element("DATA_WYSTAWIENIA").Value,
                              data_operacji = item.Element("DATA_OPERACJI").Value,
                              oper_status = Convert.ToInt32(item.Element("OPER_STATUS").Value),
                              data_utworzenia_rekordu = DateTime.Now
                          }).SingleOrDefault();

            var positions = new List<GoodsIssuePosition>();

            foreach (var item in xmlResult.Descendants("POZYCJA"))
            {
                var lp = Convert.ToInt32(item.Element("LP").Value);
                var trpid = Convert.ToInt32(item.Element("TRPID").Value);
                var twrid = Convert.ToInt32(item.Element("TOWAR").Element("TWRID").Value);
                var kod = item.Element("TOWAR").Element("KOD").Value;
                var jm = item.Element("JM").Value;

                foreach (var subitem in item.Descendants("PARTIE").Elements("PARTIA"))
                {
                    var termin_waznosci = subitem.Element("TRWALOSC").Value;
                    var magazyn = subitem.Element("MAGAZYN").Value;
                    var partia = subitem.Element("LOT").Value;

                    foreach (var subitem2 in subitem.Descendants("STRUKTURA_PAKOWANIA").Elements("PAKOWANIE"))
                    {
                        var position = new GoodsIssuePosition()
                        {
                            trnid = header.trnid,
                            jm = jm,
                            kod = kod,
                            lp = lp,
                            trpid = trpid,
                            twrid = twrid,

                            termin_waznosci = termin_waznosci,
                            magazyn = magazyn,
                            partia = partia,

                            opk_ilosc = Convert.ToDouble(subitem2.Element("OPK_ILOSC").Value.Replace('.', ',')),
                            opk_kod = subitem2.Element("OPK_KOD").Value,
                            opk_liczba = Convert.ToInt32(subitem2.Element("OPK_LICZBA").Value),
                            opk_liczba_suma = Convert.ToDouble(subitem2.Element("ILOSC_SUMA").Value.Replace('.', ',')),
                            ilosc = Convert.ToDouble(subitem2.Element("ILOSC_SUMA").Value.Replace('.', ',')),
                            paleta = subitem2.Element("JEDNOSTKA").Value,
                            liczba_palet = Convert.ToInt32(subitem2.Element("LICZBA_JEDNOSTEK").Value),
                        };
                        positions.Add(position);
                    }
                }
            }
            _repository.CreateGoodsIssue(header, positions);
        }

        public void GetGoodsIssueAdviceStatus()
        {
            var documentList = _repository.GetOpenDeliveryNote();

            if (documentList == null)
                return;

            foreach (var document in documentList)
            {
                var executeDate = DateTime.Now;

                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("NAGLOWEK",
                                new XElement("FILTR",
                                    new XElement("TRNID", document)
                                )
                            )
                        )
                    );

                var result = _client.getXmlOperationsData(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var shouldBeCreatedGoodsIssue = xmlResult.Descendants("NAGLOWEK").Skip(1).Any(i => i.Element("OPER_STATUS").Value == "40" && i.Element("TYP_DOKUMENTU").Value == "400");

                if (!shouldBeCreatedGoodsIssue)
                    continue;

                CreateGoodsIssue(xmlResult);
                SaveResult("createGoodsIssue", xmlResult, executeDate);
            }
        }

        #endregion


        #region Dokument ZWZ

        public void ReturnGoodsNoteAdvice(int nr_dokumentu)
        {
            var executeDate = DateTime.Now;

            var deliveryOrder = _repository.GetReturnDeliveryOrder(nr_dokumentu);

            if (deliveryOrder != null)
            {
                XDocument request =
                    new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("GENERATOR", deliveryOrder.Generator),
                            new XElement("TYP_DOKUMENTU", deliveryOrder.TypDokumentu),
                            new XElement("NUMER_PELNY", deliveryOrder.NumerPelny),
                            new XElement("DATA_DOKUMENTU", deliveryOrder.DataDokumentu),
                            new XElement("DATA_WYSTAWIENIA", deliveryOrder.DataWystawienia),
                            new XElement("DATA_OPERACJI", deliveryOrder.DataOperacji),
                            new XElement("OPIS", deliveryOrder.Opis),
                            new XElement("KONTRAHENT",
                                new XElement("KOD", deliveryOrder.Kod),
                                new XElement("NIP", deliveryOrder.NIP),
                                new XElement("NAZWA", deliveryOrder.Nazwa),
                                new XElement("ADRES",
                                    new XElement("KOD_POCZTOWY", deliveryOrder.KodPocztowy),
                                    new XElement("MIASTO", deliveryOrder.Miasto),
                                    new XElement("ULICA", deliveryOrder.Ulica),
                                    new XElement("KRAJ", deliveryOrder.Kraj)
                                )
                            )
                        )
                    )
                );

                var result = _client.generateXmlAdvice(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var isCorrect = xmlResult.Descendants("ROOT").Any(i => i.Element("STATUS").Value == "OK");

                if (isCorrect)
                {
                    var advice = (from item in xmlResult.Descendants("ROOT")
                                  select new
                                  {
                                      Id = Convert.ToInt32(item.Element("ID").Value),
                                      Sygnatura = item.Element("SYGNATURA").Value
                                  }).SingleOrDefault();

                    ReturnGoodsDeliveryPositions(executeDate, nr_dokumentu, advice.Id);
                    SetAdviceStatus(advice.Id);
                    _repository.SetExportStatusOnReturnDeliveryNote(nr_dokumentu, advice.Id);
                }

                SaveResult("goodsReturnNoteAdviceHeader", xmlResult, executeDate);
            }
        }

        private void ReturnGoodsDeliveryPositions(DateTime executeDate, int nr_dokumentu, int trnid)
        {
            var deliveryOrderPositions = _repository.GetReturnDeliveryOrderPosition(nr_dokumentu);

            foreach (var item in deliveryOrderPositions)
            {
                ProductSynchronizeSingle(item.Kod);
            }

            deliveryOrderPositions = _repository.GetReturnDeliveryOrderPosition(nr_dokumentu);

            XDocument request =
                new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("TRNID", trnid)
                        ),
                        new XElement("POZYCJE",
                        from item in deliveryOrderPositions
                        select
                            new XElement("POZYCJA",
                                new XElement("LP", item.Lp),
                                new XElement("PARTIA", item.Partia),
                                new XElement("TWRID", item.Twrid),
                                new XElement("ILOSC", item.Ilosc),
                                new XElement("JM", item.Jm)
                            )
                        )
                    )
                );

            var result = _client.addXmlAdvicePositions(ConvertRequest(request.ToString()), _authDataObj);

            var xmlResult = CreateXML(result);

            SaveResult("goodsReturnNoteAdvicePositions", xmlResult, executeDate);
        }

        private void CreateReturnGoodsIssue(XDocument xmlResult)
        {
            var header = (from item in xmlResult.Descendants("NAGLOWEK").Skip(1)
                          select new GoodsIssue
                          {
                              trnid = Convert.ToInt32(item.Element("TRNID").Value),
                              typ_dokumentu = Convert.ToInt32(item.Element("TYP_DOKUMENTU").Value),
                              symbol = "ZWZ",
                              numer_pelny = item.Element("NUMER_PELNY").Value,
                              data_dokumentu = item.Element("DATA_DOKUMENTU").Value,
                              data_wystawienia = item.Element("DATA_WYSTAWIENIA").Value,
                              data_operacji = item.Element("DATA_OPERACJI").Value,
                              oper_status = Convert.ToInt32(item.Element("OPER_STATUS").Value),
                              data_utworzenia_rekordu = DateTime.Now
                          }).SingleOrDefault();

            var positions = new List<GoodsIssuePosition>();

            foreach (var item in xmlResult.Descendants("POZYCJA"))
            {
                var lp = Convert.ToInt32(item.Element("LP").Value);
                var trpid = Convert.ToInt32(item.Element("TRPID").Value);
                var twrid = Convert.ToInt32(item.Element("TOWAR").Element("TWRID").Value);
                var kod = item.Element("TOWAR").Element("KOD").Value;
                var jm = item.Element("JM").Value;

                foreach (var subitem in item.Descendants("PARTIE").Elements("PARTIA"))
                {
                    var position = new GoodsIssuePosition()
                    {
                        ilosc = Convert.ToDouble(subitem.Element("ILOSC").Value.Replace('.', ',')),
                        jm = jm,
                        kod = kod,
                        lp = lp,
                        magazyn = subitem.Element("MAGAZYN").Value,
                        opk_ilosc = Convert.ToDouble(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_ILOSC").Value.Replace('.', ',')),
                        opk_kod = subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_KOD").Value,
                        opk_liczba = Convert.ToInt32(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_LICZBA").Value),
                        opk_liczba_suma = Convert.ToDouble(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("ILOSC_SUMA").Value.Replace('.', ',')),
                        paleta = subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("JEDNOSTKA").Value,
                        liczba_palet = Convert.ToInt32(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("LICZBA_JEDNOSTEK").Value),
                        partia = subitem.Element("LOT").Value,
                        termin_waznosci = subitem.Element("TRWALOSC").Value,
                        trnid = header.trnid,
                        trpid = trpid,
                        twrid = twrid
                    };

                    positions.Add(position);
                }
            }
            _repository.CreateGoodsIssue(header, positions);
        }

        public void GetReturnGoodsIssueAdviceStatus()
        {
            var documentList = _repository.GetOpenReturnDeliveryNote();

            if (documentList == null)
                return;

            foreach (var document in documentList)
            {
                var executeDate = DateTime.Now;

                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("NAGLOWEK",
                                new XElement("FILTR",
                                    new XElement("TRNID", document)
                                )
                            )
                        )
                    );

                var result = _client.getXmlOperationsData(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var shouldBeCreatedReturnGoodsIssue = xmlResult.Descendants("NAGLOWEK").Skip(1).Any(i => i.Element("OPER_STATUS").Value == "40" && i.Element("TYP_DOKUMENTU").Value == "400");

                if (!shouldBeCreatedReturnGoodsIssue)
                    continue;

                CreateReturnGoodsIssue(xmlResult);
                SaveResult("createReturnGoodsIssue", xmlResult, executeDate);
            }
        }

        #endregion

        #region Dokument WZ

        public void DeliveryNoteAdvice(int nr_dokumentu)
        {
            var executeDate = DateTime.Now;

            var transportOrder = _repository.GetTransportOrder(nr_dokumentu);

            if (transportOrder != null)
            {
                XDocument request =
                    new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("GENERATOR", transportOrder.Generator),
                            new XElement("TYP_DOKUMENTU", transportOrder.Typ_Dokumentu),
                            new XElement("NUMER_PELNY", transportOrder.Numer_Pelny),
                            new XElement("DATA_DOKUMENTU", transportOrder.Data_Dokumentu),
                            new XElement("DATA_WYSTAWIENIA", transportOrder.Data_Wystawienia),
                            new XElement("DATA_OPERACJI", transportOrder.Data_Operacji),
                            new XElement("OPIS", transportOrder.Opis),
                            new XElement("UWAGI_MAGAZYN", transportOrder.Uwagi_Magazyn),
                            new XElement("OSOBA_KONTAKT", transportOrder.Osoba_Kontakt),
                            new XElement("OSOBA_TELEFON", transportOrder.Osoba_Telefon),
                            new XElement("PRZEWOZNIK", transportOrder.Przewoznik),
                            new XElement("KONTRAHENT",
                                new XElement("KOD", transportOrder.Kod),
                                new XElement("NIP", transportOrder.NIP),
                                new XElement("NAZWA", transportOrder.Nazwa),
                                new XElement("ADRES",
                                    new XElement("KOD_POCZTOWY", transportOrder.Kod_Pocztowy),
                                    new XElement("MIASTO", transportOrder.Miasto),
                                    new XElement("ULICA", transportOrder.Ulica),
                                    new XElement("KRAJ", transportOrder.Kraj)
                                )
                            ),
                            transportOrder.Nazwa_Dostawa.Length > 0 ? new XElement("DOSTAWA",
                                new XElement("NAZWA", transportOrder.Nazwa_Dostawa),
                                new XElement("KOD_POCZTOWY", transportOrder.Kod_Pocztowy_Dostawa),
                                new XElement("MIASTO", transportOrder.Miasto_Dostawa),
                                new XElement("ULICA", transportOrder.Ulica_Dostawa),
                                new XElement("KRAJ", transportOrder.Kraj_Dostawa)
                            ) : null
                        )
                    )
                );

                var result = _client.generateXmlAdvice(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var isCorrect = xmlResult.Descendants("ROOT").Any(i => i.Element("STATUS").Value == "OK");

                if (isCorrect)
                {
                    var advice = (from item in xmlResult.Descendants("ROOT")
                                  select new
                                  {
                                      Id = Convert.ToInt32(item.Element("ID").Value),
                                      Sygnatura = item.Element("SYGNATURA").Value
                                  }).SingleOrDefault();

                    TransportOrderPositions(executeDate, nr_dokumentu, advice.Id);
                    SetAdviceStatus(advice.Id);
                    _repository.SetExportStatusOnTransportOrder(nr_dokumentu, advice.Id);
                }

                SaveResult("deliveryNoteAdviceHeader", xmlResult, executeDate);
            }
        }

        private void TransportOrderPositions(DateTime executeDate, int nr_dokumentu, int trnid)
        {
            var transportOrderPositions = _repository.GetTransportOrderPosition(nr_dokumentu);

            XDocument request =
                new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("TRNID", trnid)
                        ),
                        new XElement("POZYCJE",
                        from item in transportOrderPositions
                        select
                            new XElement("POZYCJA",
                                new XElement("LP", item.Lp),
                                new XElement("TWRID", item.Twrid),
                                new XElement("ILOSC", item.Ilosc),
                                new XElement("PARTIA", item.Partia),
                                new XElement("JM", item.Jm)
                            )
                        )
                    )
                );

            var result = _client.addXmlAdvicePositions(ConvertRequest(request.ToString()), _authDataObj);

            var xmlResult = CreateXML(result);

            SaveResult("deliveryNoteAdvicePositions", xmlResult, executeDate);
        }

        public void GetDeliveryAdviceStatus()
        {
            var documentList = _repository.GetOpenTransportOrder();

            if (documentList == null)
                return;

            foreach (var document in documentList)
            {
                var executeDate = DateTime.Now;

                XDocument request =
                    new XDocument(
                        new XElement("ROOT",
                            new XElement("NAGLOWEK",
                                new XElement("FILTR",
                                    new XElement("TRNID", document)
                                )
                            )
                        )
                    );

                var result = _client.getXmlOperationsData(ConvertRequest(request.ToString()), _authDataObj);

                var xmlResult = CreateXML(result);

                var shouldBeCreatedGoodsIssue = xmlResult.Descendants("NAGLOWEK").Skip(1).Any(i => i.Element("OPER_STATUS").Value == "40" && i.Element("TYP_DOKUMENTU").Value == "420");

                if (!shouldBeCreatedGoodsIssue)
                    continue;

                CreateDelivery(xmlResult);
                SaveResult("createDelivery", xmlResult, executeDate);
            }
        }

        private void CreateDelivery(XDocument xmlResult)
        {
            var header = (from item in xmlResult.Descendants("NAGLOWEK").Skip(1)
                          select new Delivery
                          {
                              trnid = Convert.ToInt32(item.Element("TRNID").Value),
                              typ_dokumentu = Convert.ToInt32(item.Element("TYP_DOKUMENTU").Value),
                              symbol = "WZ",
                              numer_pelny = item.Element("NUMER_PELNY").Value,
                              data_dokumentu = item.Element("DATA_DOKUMENTU").Value,
                              data_wystawienia = item.Element("DATA_WYSTAWIENIA").Value,
                              data_operacji = item.Element("DATA_OPERACJI").Value,
                              oper_status = Convert.ToInt32(item.Element("OPER_STATUS").Value),
                              data_utworzenia_rekordu = DateTime.Now
                          }).SingleOrDefault();

            var positions = new List<DeliveryPosition>();

            foreach (var item in xmlResult.Descendants("POZYCJA"))
            {
                var lp = Convert.ToInt32(item.Element("LP").Value);
                var trpid = Convert.ToInt32(item.Element("TRPID").Value);
                var twrid = Convert.ToInt32(item.Element("TOWAR").Element("TWRID").Value);
                var kod = item.Element("TOWAR").Element("KOD").Value;
                var jm = item.Element("JM").Value;

                foreach (var subitem in item.Descendants("PARTIE").Elements("PARTIA"))
                {
                    var position = new DeliveryPosition()
                    {
                        ilosc = Convert.ToDouble(subitem.Element("ILOSC").Value.Replace('.', ',')),
                        jm = jm,
                        kod = kod,
                        lp = lp,
                        magazyn = subitem.Element("MAGAZYN").Value,
                        opk_ilosc = Convert.ToDouble(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_ILOSC").Value.Replace('.', ',')),
                        opk_kod = subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_KOD").Value,
                        opk_liczba = Convert.ToInt32(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("OPK_LICZBA").Value),
                        opk_liczba_suma = Convert.ToDouble(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("ILOSC_SUMA").Value.Replace('.', ',')),
                        paleta = subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("JEDNOSTKA").Value,
                        liczba_palet = Convert.ToInt32(subitem.Element("STRUKTURA_PAKOWANIA").Element("PAKOWANIE").Element("LICZBA_JEDNOSTEK").Value),
                        partia = subitem.Element("LOT").Value,
                        termin_waznosci = subitem.Element("TRWALOSC").Value,
                        trnid = header.trnid,
                        trpid = trpid,
                        twrid = twrid
                    };

                    positions.Add(position);
                }
            }

            var freightPositions = new List<FreightPosition>();

            foreach (var item in xmlResult.Descendants("LADUNEK"))
            {
                var position = new FreightPosition()
                {
                    trnid = header.trnid,
                    liczba = Convert.ToInt32(item.Element("LICZBA").Value),
                    rodzaj = item.Element("RODZAJ").Value,
                    waga = Convert.ToDouble(item.Element("WAGA").Value.Replace('.', ',')),
                    wagajm = item.Element("WAGAJM").Value
                };

                freightPositions.Add(position);
            }

            _repository.CreateDelivery(header, positions, freightPositions);
        }

        public void SendDeliveryAdviceNumber()
        {
            var documentList = _repository.GetOpenTransportOrder();

            if (documentList == null)
                return;

            foreach (var trnid in documentList)
            {
                var adviceNumber = _repository.GetAdviceNumber(trnid);

                if (!String.IsNullOrEmpty(adviceNumber))
                {
                    XDocument request =
                        new XDocument(
                        new XElement("ROOT",
                            new XElement("NAGLOWEK",
                                new XElement("GENERATOR", "ANTEEO_EXCHANGER"),
                                new XElement("TRNID", trnid),
                                new XElement("NUMER_PELNY", adviceNumber)
                            )
                        )
                    );

                    var executeDate = DateTime.Now;

                    var result = _client.modifyXmlAdvice(ConvertRequest(request.ToString()), _authDataObj);

                    //Nie zapisuje informacji o wysłaniu numerów WZ wystawionych w Graffiti
                    var xmlResult = CreateXML(result);

                    //var isCorrect = xmlResult.Descendants("ROOT").Descendants("STATUS").Any(i => i.Element("KOD").Value == "OK");

                    //if (!isCorrect)
                    //    continue;

                    //SaveResult("sendDeliveryAdviceNumber", xmlResult, executeDate);
                }
            }
        }


        public void AddDocuments(int nr_dokumentu, int trnid, int documentType)
        {
            // documentType = 1 - dokumenty jakościowe
            // documentType = 2 - faktura

            var documents = _repository.GetGraffitiDocument(nr_dokumentu, documentType);

            if (trnid == 0)
            {
                trnid = _repository.GetDeliveryNote(nr_dokumentu);
            }

            if (documents != null && trnid > 0)
            {
                foreach (var document in documents)
                {
                    var base64 = Convert.ToBase64String(document.Obiekt, Base64FormattingOptions.None);
                    var md5 = Helper.MD5Hash(document.Obiekt);
                    var maxSizeOfMessage = 15000;
                    var loopsCount = Math.Ceiling(base64.Length / (decimal)maxSizeOfMessage);
                    var executeDate = DateTime.Now;
                    var executeDateAsText = executeDate.ToString("yyyyMMdd_HHmmss");

                    for (int i = 0; i < loopsCount; i++)
                    {
                        var part = base64.SafeSubstring(i * maxSizeOfMessage, maxSizeOfMessage);
                        XDocument request =
                            new XDocument(
                                new XElement("ROOT",
                                    new XElement("NAGLOWEK",
                                        new XElement("TRNID", trnid),
                                            new XElement("PLIK",
                                                new XElement("LP", i + 1),
                                                i == 0 || i == loopsCount - 1 ? new XElement("NAZWA", executeDateAsText + "-" + document.IdentyfikatorPliku) : null,
                                                new XElement("RODZAJ", "B"),
                                                i == loopsCount - 1 ? new XElement("MD5", md5) : null,
                                                new XElement("CIAGBASE64", part)
                                            )
                                    )
                                )
                            );

                        var result = _client.addXmlOperationsFile(ConvertRequest(request.ToString()), _authDataObj);

                        if (i == loopsCount - 1)
                        {
                            var xmlResult = CreateXML(result);
                            SaveResult("addDocuments", xmlResult, executeDate);
                        }
                    }
                }
            }
        }
        #endregion

        private void SetAdviceStatus(int trnid)
        {
            XDocument request =
                new XDocument(
                    new XElement("ROOT",
                        new XElement("NAGLOWEK",
                            new XElement("TRNID", trnid),
                            new XElement("STATUS", "20")
                        )
                    )
                );

            var result = _client.setXmlAdviceStatus(ConvertRequest(request.ToString()), _authDataObj);
        }

        private byte[] ConvertRequest(string xmlRequest)
        {
            return Encoding.UTF8.GetBytes(xmlRequest.ToString());
        }

        private string ConvertResult(byte[] result)
        {
            return Encoding.UTF8.GetString(result);
        }

        private XDocument CreateXML(byte[] result)
        {
            return XDocument.Parse(ConvertResult(result));
        }

        private void SaveResult(string name, XDocument content, DateTime executeDate)
        {
            string fileName = name + "_" + executeDate.ToString("yyyyMMdd_HHmmssffff") + ".xml";

            content.Save(@"\\SERVER_IP\Graffiti\Graffiti\WCF\xml\" + fileName);
        }
    }
}
