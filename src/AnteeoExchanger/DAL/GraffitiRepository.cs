using AnteeoExchanger.Models;
using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;

namespace AnteeoExchanger.DAL
{
    public class GraffitiRepository
    {
        private string _connectionString = "User ID=user;Host=8.8.8.8;Port=5432;Encoding=windows-1250;Client Encoding=latin2;Application Name=AnteeoExchanger;Database=database;Pooling=true;";

        internal IDbConnection Connection
        {
            get
            {
                return new NpgsqlConnection(_connectionString);
            }
        }

        public IEnumerable<Product> GetAllProducts()
        {
            var sql = "SELECT htx_id_wms as id, TRIM(indeks) AS kod, TRIM(nazwa_indeksu) AS nazwa, 'SPOŻYWCZE' AS asortyment, id_indeksu AS refid, TRIM(jm) AS jm, '' as ean, '' as opis FROM g.gm_indeksy WHERE typ = 0 AND pole3 = 0 AND klasyfikacjatowaru IN (0, 1, 2, 5) AND indeks <> '' AND rodzaj_materialu3 NOT IN (42, 45, 46, 43, 44, 50);";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<Product>(sql);
            }
        }

        public TransportOrder GetTransportOrder(int nr_dokumentu)
        {
            var sql = @"SELECT
                        'AnteeoExchanger' AS generator
                        , 420 AS typ_dokumentu
                        , p.id AS numer_pelny
                        , TO_CHAR(g.datasql(p.data_zlecenia), 'YYYY-MM-DD') as data_dokumentu
                        , TO_CHAR(g.datasql(p.data_zlecenia), 'YYYY-MM-DD') as data_wystawienia
                        , TO_CHAR(g.datasql(p.data_podjecia), 'YYYY-MM-DD') as data_operacji
                        , build.htx_zlecenie_transportowe_info(1, @nr_dokumentu) AS opis
                        , build.htx_zlecenie_transportowe_info(2, @nr_dokumentu) AS uwagi_magazyn
                        , LEFT(TRIM(t.odbiorca_telefon), 30) AS osoba_telefon
                        , LEFT(TRIM(t.odbiorca_osoba_kontakt), 100) AS osoba_kontakt
                        , CASE WHEN p.id_przewoznika = 1607 THEN 'SCHENKER' ELSE 'ODBIOR_WLASNY' END AS przewoznik                        
						, p.id_nabywcy AS kod
                        , TRIM(CASE WHEN k.nip = '' THEN k.nip_ue ELSE k.nip END) AS nip
                        , CASE WHEN p.id_odbiorcy = p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_nazwa), 100) ELSE LEFT(TRIM(k.nazwa_kontrahenta), 100) END AS nazwa
                        , CASE WHEN p.id_odbiorcy = p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_kod_pocztowy), 10) ELSE LEFT(TRIM(k.kod_miasta), 10) END AS kod_pocztowy
                        , CASE WHEN p.id_odbiorcy = p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_miasto), 50) ELSE LEFT(TRIM(k.miasto), 50) END AS miasto
                        , CASE WHEN p.id_odbiorcy = p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_adres), 50) ELSE LEFT(TRIM(k.adres), 50) END AS ulica
                        , CASE WHEN p.id_odbiorcy = p.id_nabywcy THEN kr_odbiorca.kraj ELSE kr_nabywca.kraj END AS kraj                        
						, CASE WHEN p.id_odbiorcy <> p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_nazwa), 100) ELSE '' END AS nazwa_dostawa
                        , CASE WHEN p.id_odbiorcy <> p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_kod_pocztowy), 10) ELSE '' END AS kod_pocztowy_dostawa
                        , CASE WHEN p.id_odbiorcy <> p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_miasto), 50) ELSE '' END AS miasto_dostawa
                        , CASE WHEN p.id_odbiorcy <> p.id_nabywcy THEN LEFT(TRIM(t.odbiorca_adres), 50) ELSE '' END AS ulica_dostawa
                        , CASE WHEN p.id_odbiorcy <> p.id_nabywcy THEN kr_nabywca.kraj ELSE '' END AS kraj_dostawa						
                        FROM build.mzk_zlecenie_pakowania p
                        LEFT JOIN build.mzk_zlecenie_transport t ON(p.id = t.nr_zlec_pak)
                        LEFT JOIN g.spd_kontrahenci k ON(p.id_nabywcy = k.id_kontrahenta)
                        LEFT JOIN g.spd_kontrah_adresy a ON(p.id_odbiorcy = a.id_kontrah AND p.lp_adresu_odb = a.lp)
						LEFT OUTER JOIN
						(select id_kontrahenta, kod_intrastat as kraj from g.spd_kraje as kraj left outer join g.spd_kontrahenci_info_dodatkow as kontr on (kraj.kraj = kontr.pole_s1 ) where rodzaj_info = 28) AS kr_nabywca
						on (p.id_nabywcy = kr_nabywca.id_kontrahenta)
						LEFT OUTER JOIN
						(select id_kontrahenta, kod_intrastat as kraj from g.spd_kraje as kraj left outer join g.spd_kontrahenci_info_dodatkow as kontr on (kraj.kraj = kontr.pole_s1 ) where rodzaj_info = 28) AS kr_odbiorca
						on (p.id_odbiorcy = kr_odbiorca.id_kontrahenta)						
                        WHERE p.id = @nr_dokumentu; ";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.QuerySingleOrDefault<TransportOrder>(sql, new { nr_dokumentu });
            }
        }

        public string GetAdviceNumber(int nr_dokumentu)
        {
            var sql_wz = @"SELECT  id || ';' || build.htx_zlecenie_transportowe_numer_wz(@nr_dokumentu) FROM build.mzk_zlecenie_pakowania WHERE htx_id_awiza_wms = @nr_dokumentu";
            var sql_nr_zam = @"SELECT * FROM build.htx_zlecenie_transportowe_zamowienie_klienta_nr(@nr_dokumentu)";

            using (IDbConnection db = Connection)
            {
                db.Open();
                var result1 = db.QuerySingleOrDefault<string>(sql_wz, new { nr_dokumentu });
                var result2 = db.QuerySingleOrDefault<string>(sql_nr_zam, new { nr_dokumentu });
                return String.Concat(result1, ";", result2);
            }
        }

        public Product GetSingleProduct(string kod)
        {
            var sql = "SELECT TRIM(indeks) AS kod, TRIM(nazwa_indeksu) AS nazwa, 'SPOŻYWCZE' AS asortyment, id_indeksu AS refid, TRIM(jm) AS jm, '' as ean, '' as opis FROM g.gm_indeksy WHERE indeks = @Kod;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.QuerySingleOrDefault<Product>(sql, new { kod });
            }
        }

        public void UpdateProduct(string kod, int twrid)
        {
            var sql = "UPDATE g.gm_indeksy SET htx_id_wms = @Twrid WHERE indeks = @Kod;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                db.Execute(sql, new { kod, twrid });
            }
        }

        public DeliveryOrder GetDeliveryOrder(int rok, int rodzaj, int nr)
        {
            var sql = "SELECT 'AnteeoExchanger' AS generator, 400 AS typdokumentu, d.nr_zamowienia || '/' || ABS(d.rodzaj) || '/' || d.rok_zamowienia AS numerpelny, TO_CHAR(g.datasql(d.data_zamowienia), 'YYYY-MM-DD') as datadokumentu, TO_CHAR(g.datasql(d.data_zamowienia), 'YYYY-MM-DD') as datawystawienia, TO_CHAR(g.datasql(d.data_realizacji_potw), 'YYYY-MM-DD') as dataoperacji, '' AS opis, d.id_kontrahenta AS kod, TRIM(CASE WHEN k.nip = '' THEN k.nip_ue ELSE k.nip END) AS nip, LEFT(TRIM(k.nazwa_kontrahenta), 100) AS nazwa, LEFT(TRIM(k.kod_miasta), 10) AS kodpocztowy, LEFT(TRIM(k.miasto), 50) AS miasto, LEFT(TRIM(k.adres), 50) AS ulica, p.kod_intrastat AS kraj FROM g.mzk_zamow_dostawy d LEFT JOIN g.spd_kontrahenci k ON (d.id_kontrahenta = k.id_kontrahenta) LEFT JOIN g.spd_kontrahenci_info_dodatkow i ON (d.id_kontrahenta = i.id_kontrahenta AND i.rodzaj_info = 28) LEFT JOIN g.spd_kraje p ON (i.pole_s1 = p.kraj) WHERE d.rok_zamowienia = @rok AND d.rodzaj = @rodzaj AND d.nr_zamowienia = @nr";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.QuerySingleOrDefault<DeliveryOrder>(sql, new { rok, rodzaj, nr });
            }
        }

        public ReturnDeliveryOrder GetReturnDeliveryOrder(int nr)
        {
            var sql = @"SELECT 
                'AnteeoExchanger' AS generator 
                , 400 AS typdokumentu 
                , d.numer || '/' || d.symbol || '/' || d.rok AS numerpelny 
                , TO_CHAR(g.datasql(d.data), 'YYYY-MM-DD') as datadokumentu 
                , TO_CHAR(g.datasql(d.data_wystaw), 'YYYY-MM-DD') as datawystawienia 
                , TO_CHAR(g.datasql(d.data), 'YYYY-MM-DD') as dataoperacji 
                , '' AS opis 
                , d.kontrahent AS kod 
                , TRIM(CASE WHEN k.nip = '' THEN k.nip_ue ELSE k.nip END) AS nip 
                , LEFT(TRIM(k.nazwa_kontrahenta), 100) AS nazwa 
                , LEFT(TRIM(k.kod_miasta), 10) AS kodpocztowy 
                , LEFT(TRIM(k.miasto), 50) AS miasto 
                , LEFT(TRIM(k.adres), 50) AS ulica 
                , p.kod_intrastat AS kraj 
                FROM g.gm_dokumenty_naglowki d 
                LEFT JOIN g.spd_kontrahenci k ON (d.kontrahent = k.id_kontrahenta) 
                LEFT JOIN g.spd_kontrahenci_info_dodatkow i ON (d.kontrahent = i.id_kontrahenta AND i.rodzaj_info = 28) 
                LEFT JOIN g.spd_kraje p ON (i.pole_s1 = p.kraj) 
                WHERE d.id = @nr";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.QuerySingleOrDefault<ReturnDeliveryOrder>(sql, new { nr });
            }
        }

        public IEnumerable<TransportOrderPosition> GetTransportOrderPosition(int nr_dokumentu)
        {
            var sql = "SELECT p.lp AS lp, i.htx_id_wms AS twrid, p.ilosc_zamowiona AS ilosc, TRIM(i.jm) AS jm, TRIM(p.partia_towaru) AS partia FROM build.mzk_zlecenia_pak_zam_poz p LEFT JOIN g.gm_indeksy i ON(i.id_indeksu = p.id_towaru) WHERE p.id_pak = @nr_dokumentu";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<TransportOrderPosition>(sql, new { nr_dokumentu });
            }
        }

        public IEnumerable<DeliveryOrderPosition> GetDeliveryOrderPosition(int rok, int nr)
        {
            var sql = "SELECT p.lp as lp, i.htx_id_wms as twrid, p.ilosc as ilosc, TRIM(i.jm) as jm, TRIM(p.indeks) as kod FROM g.mzk_zamow_dostawy_pozycje p LEFT JOIN g.gm_indeksy i ON(i.id_indeksu = p.id_materialu) WHERE p.rok_zamowienia = @rok AND p.nr_zamowienia = @nr";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<DeliveryOrderPosition>(sql, new { rok, nr });
            }
        }

        public IEnumerable<ReturnDeliveryOrderPosition> GetReturnDeliveryOrderPosition(int nr)
        {
            var sql = @"SELECT
                p.lp as lp
                , i.htx_id_wms as twrid
                , p.ilosc as ilosc
                , TRIM(p.jm_wej) as jm
                , TRIM(i.indeks) as kod
                , LEFT(TRIM(p.atest),30) as partia
                FROM g.gm_dokumenty_pozycje p
                LEFT JOIN g.gm_indeksy i ON(i.id_indeksu = p.id_indeksu)
                WHERE p.id_naglowka = @nr";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<ReturnDeliveryOrderPosition>(sql, new { nr });
            }
        }

        public void SetExportStatusOnDeliveryNote(int rok, int nr, int rodzaj, int twrid)
        {
            var sql = "UPDATE g.mzk_zamow_dostawy SET htx_id_awiza_wms = @Twrid, pole3 = 1, htx_status_awiza_wms = 30 WHERE rok_zamowienia = @rok AND rodzaj = @rodzaj AND nr_zamowienia = @nr;"
                    + "UPDATE g.mzk_zamow_dostawy_pozycje SET htx_id_awiza_wms = @Twrid WHERE rok_zamowienia = @rok AND nr_zamowienia = @nr;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                db.Execute(sql, new { rok, nr, rodzaj, twrid });
            }
        }

        public void SetExportStatusOnReturnDeliveryNote(int nr, int twrid)
        {
            var sql = "UPDATE g.gm_dokumenty_naglowki SET htx_id_awiza_wms = @Twrid, hort_wyslany = 1, htx_status_awiza_wms = 30 WHERE id = @nr;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                db.Execute(sql, new { nr, twrid });
            }
        }

        public IEnumerable<int> GetOpenDeliveryNote()
        {
            var sql = "SELECT htx_id_awiza_wms FROM g.mzk_zamow_dostawy WHERE htx_status_awiza_wms = 30 AND htx_id_awiza_wms > 0";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<int>(sql);
            }
        }

        public IEnumerable<int> GetOpenReturnDeliveryNote()
        {
            var sql = "SELECT htx_id_awiza_wms FROM g.gm_dokumenty_naglowki WHERE htx_status_awiza_wms = 30 AND htx_id_awiza_wms > 0";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<int>(sql);
            }
        }

        public void CreateGoodsIssue(GoodsIssue header, List<GoodsIssuePosition> positions)
        {
            var sqlHeader = "INSERT INTO build.mzk_awizo_wms_naglowek VALUES(@trnid, @typ_dokumentu, @symbol, to_date(@data_dokumentu, 'YYYY-MM-DD'), to_date(@data_wystawienia, 'YYYY-MM-DD'), to_date(@data_operacji, 'YYYY-MM-DD'), @oper_status, @data_utworzenia_rekordu, @numer_pelny);";
            var sqlPosition = "INSERT INTO build.mzk_awizo_wms_pozycje VALUES(@trnid, @lp, @trpid, @twrid, @kod, @jm, @partia, @ilosc, @opk_kod, @opk_liczba, @opk_liczba_suma, @paleta, to_date(@termin_waznosci, 'YYYY-MM-DD'), @magazyn, @opk_ilosc, @liczba_palet);";

            var sql = "";

            if (header.symbol == "PZ")
            {
                sql = "UPDATE g.mzk_zamow_dostawy SET htx_status_awiza_wms = 40 WHERE htx_id_awiza_wms = @trnid;";
            }

            if (header.symbol == "ZWZ")
            {
                sql = "UPDATE g.gm_dokumenty_naglowki SET htx_status_awiza_wms = 40 WHERE htx_id_awiza_wms = @trnid;";
            }

            using (IDbConnection db = Connection)
            {
                db.Open();
                var trans = db.BeginTransaction();
                db.Execute(sqlHeader, header);

                foreach (var position in positions)
                {
                    db.Execute(sqlPosition, position);
                }

                db.Execute(sql, new { trnid = header.trnid });

                trans.Commit();
            }
        }

        public void CreateDelivery(Delivery header, List<DeliveryPosition> positions, List<FreightPosition> freightPositions)
        {
            var sqlHeader = "INSERT INTO build.mzk_awizo_wms_naglowek VALUES(@trnid, @typ_dokumentu, @symbol, to_date(@data_dokumentu, 'YYYY-MM-DD'), to_date(@data_wystawienia, 'YYYY-MM-DD'), to_date(@data_operacji, 'YYYY-MM-DD'), @oper_status, @data_utworzenia_rekordu, @numer_pelny);";
            var sqlPosition = "INSERT INTO build.mzk_awizo_wms_pozycje VALUES(@trnid, @lp, @trpid, @twrid, @kod, @jm, @partia, @ilosc, @opk_kod, @opk_liczba, @opk_liczba_suma, @paleta, to_date(@termin_waznosci, 'YYYY-MM-DD'), @magazyn, @opk_ilosc, @liczba_palet);";
            var sqlFreight = "INSERT INTO build.mzk_awizo_wms_ladunek VALUES(@trnid, @rodzaj, @liczba, @waga, @wagajm);";
            var sql = "UPDATE build.mzk_zlecenie_pakowania SET htx_status_awiza_wms = 40 WHERE htx_id_awiza_wms = @trnid";

            using (IDbConnection db = Connection)
            {
                db.Open();
                var trans = db.BeginTransaction();
                db.Execute(sqlHeader, header);

                foreach (var position in positions)
                {
                    db.Execute(sqlPosition, position);
                }

                foreach (var position in freightPositions)
                {
                    db.Execute(sqlFreight, position);
                }

                db.Execute(sql, new { trnid = header.trnid });

                trans.Commit();
            }
        }

        public void SetExportStatusOnTransportOrder(int nr_dokumentu, int twrid)
        {
            var sql = "UPDATE build.mzk_zlecenie_pakowania SET htx_id_awiza_wms = @Twrid, htx_status_awiza_wms = 30, htx_export_awiza_wms = 1, status = 2 WHERE id = @nr_dokumentu;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                db.Execute(sql, new { nr_dokumentu, twrid });
            }
        }

        public IEnumerable<int> GetOpenTransportOrder()
        {
            var sql = "SELECT htx_id_awiza_wms FROM build.mzk_zlecenie_pakowania WHERE htx_status_awiza_wms = 30 AND htx_id_awiza_wms > 0 AND g.datasql(data_podjecia) > (current_date - interval '1 month');";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<int>(sql);
            }
        }

        public IEnumerable<GraffitiDocument> GetGraffitiDocument(int nr_dokumentu, int typ_dokumentu)
        {
            //typ_dokumentu 
            // 1 - dokumenty jakościowe
            // 2 - faktury
            var sql = "SELECT identyfikatorpliku, obiekt_typ, obiekt FROM build.htx_pobierz_dokument(@nr_dokumentu, @typ_dokumentu)";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.Query<GraffitiDocument>(sql, new { nr_dokumentu, typ_dokumentu });
            }
        }

        public int GetDeliveryNote(int nr_dokumentu)
        {
            var sql = "SELECT htx_id_awiza_wms FROM build.mzk_zlecenie_pakowania WHERE id = @nr_dokumentu;";

            using (IDbConnection db = Connection)
            {
                db.Open();
                return db.ExecuteScalar<int>(sql, new { nr_dokumentu });
            }
        }
    }
}
