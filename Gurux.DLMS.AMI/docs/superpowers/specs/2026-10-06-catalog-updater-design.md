# GXUpdater ja Gurux.AMI.Catalog

## Hyväksytty tavoite

GXUpdater käyttää Gurux.Updater.Net-kirjastoa moduuli- ja agenttikatalogin
hakemiseen ja tallentaa katalogin versiot AMI:n tietokantaan. ModulesManage
näyttää myös esijulkaisuversiot ja sallii asennettavan version valinnan.
Vanhoille moduuli-/agentti-JSON-syötteille tai niiden Update-DTO:ille ei tehdä fallbackia.

Katalogin repositorio on `gurux/Gurux.AMI.Catalog`. Oletettu JSON-osoite on
`https://raw.githubusercontent.com/gurux/Gurux.AMI.Catalog/main/catalog.json`.
`main` ja `catalog.json` ovat hyväksyttäväksi esitettyjä oletuksia; repositorion
tiedostosisältöä ei ole voitu vahvistaa. Täysi katalogiosoite on vaihdettava asetus.
Toteutuksen testit käyttävät erillistä testikatalogia eivätkä edellytä repositorion
julkistamista. GitHubin HTML-sivun osoitetta ei käytetä JSON-lähteenä.

## Rajaus ja integraatio

Gurux.Updater.Net on jo hostin projektiviitteenä. Käytetään paikallisen kirjaston
GXCatalogUpdateServicea ja GXUpdateCatalog/GXUpdateCatalogItem/GXUpdateRelease-
malleja. Katalogi ladataan kerran RunAsync-päivityskierroksella ja sama snapshot
käytetään moduuleille ja agenteille. Erillinen vain-moduuli-/vain-agenttitarkistus
voi ladata oman snapshotin. HttpClient tulee IHttpClientFactoryn kautta ja
cancellation välitetään verkko- ja tietokantakutsuihin.

ProductType.Module ja ProductType.Agent käsitellään niiden omiin tauluihin.
Application-tuotteet eivät synnytä AMI-moduuli-/agenttirivejä.
Nykyinen valmistaja-/laitemallipäivitys säilytetään. Sen tietolähde ja mallit ovat
erillinen toiminto, eivät vanhan moduuli-/agenttikatalogin yhteensopivuuskerros.
GXUpdaterin vanha moduuli-/agenttimallisto ja niille osoitetut URLit poistetaan,
kun niiden käyttö on korvattu.

## Tuotetunnisteet ja versioiden tallennus

Moduulikatalogin Id vastaa AMI:n moduulin/IAmiModule.Id-tunnistetta.
Näyttönimeä tai repositoryn nimeä ei käytetä identiteetin arvaamiseen.
Uusi moduuli tallennetaan installable/inactive-tilaan; olemassa olevan asennetun
moduulin aktiivisuutta, asetuksia, asennettua versiota tai kehityspolkua ei muuteta.

Agentin tietokanta-ID on GUID, joten agenttituotteelle lisätään erillinen nullable
CatalogId-kenttä. Uusi katalogituote luo oman template/installer-agentin.
Tuotteen julkaisuja ei kopioida kaikkiin agentteihin, eikä yhden agenttituotteen
AvailableVersion saa päivittyä toisen tuotteen version mukaan. Olemassa olevien
ei-katalogipohjaisten agenttien tunnisteita tai GUIDeja ei muuteta. Automaattista
nimeen perustuvaa legacy-mappausta ei lisätä; uusi CatalogId-yhteys on nimenomainen.

Sekä vakaat että prerelease-julkaisut tallennetaan. Number tulee release.Version-
arvosta, Prerelease julkaisutiedosta, Description release notesista ja CreationTime
PublishedAt-arvosta. Ladattavan paketin Url on asset.DownloadUrl ja FileName asset.Name;
releaseUrl on HTML-julkaisusivu eikä asennuspaketin osoite.

Käytetään kirjaston assetPattern-valintaa, mutta monitulkintaista ZIP-valintaa ei
hyväksytä hiljaisesti. Moduulien/agenttien pakettivalinta on konfiguroitavissa,
jotta eri katalogituotteet ja platform-paketit voidaan erottaa. Release ilman
yksiselitteistä sopivaa pakettia ei tule asennusvalinnaksi; julkaisutiedot voidaan
silti säilyttää versiona ilman latausosoitetta. Käyttöliittymä kertoo puuttuvasta paketista.

Upsert on idempotentti tuotteen ja version mukaan. Olemassa olevan version
katalogimetadatan päivitys päivittää saman rivin, ei tee duplikaattia.
Rinnakkaiset tarkistukset eivät luo samaa tuotetta/versiota kahdesti.
Katalogista myöhemmin puuttuvia tuotteita/versioita ei poisteta automaattisesti.
Katalogivirhe ei tyhjennä viimeksi tallennettua tarjontaa.

Moduuli- ja agenttiversion Number-sarakkeen nykyinen lyhyt pituus laajennetaan
vähintään128 merkkiin, jotta esijulkaisutunnisteet mahtuvat. Skeemapäivitys säilyttää
nykyiset versiot ja tunnisteet. Luettelon onnistumisaika päivittyy vasta onnistuneen
tallennuksen jälkeen; virhe kirjataan asynkronisesti eikä piiloteta onnistumiseksi.
Taustatarkistus ei oleta HttpContextin tai kirjautuneen käyttäjän olevan saatavilla.

## Versiovertailu ja oletustarjonta

System.Versionin suoraa käyttöä prerelease-merkkijonolle ei jatketa.
Vertailu ymmärtää semanttisen version core-numerot ja prerelease-tunnisteiden
järjestyksen, esimerkiksi beta.2 < beta.10 < rc.1 < saman core-version stable.
Nykyisten asennettujen neliosaisten assembly-versioiden vertailu sallitaan;
puuttuva neljäs nolla ei muuta version järjestystä. Build metadata ei nosta
version etusijaa. Julkaisuaika ei yksin ratkaise uusinta versiota.

AvailableVersion ja normaali päivitysehdotus käyttävät uusinta sopivaa vakaata
versiota. NewVersion asetetaan vain, jos tarjolla on asennettua uudempi vakaa
versio. Esijulkaisun pelkkä löytyminen ei automaattisesti vaihda päivityskanavaa
tai käynnistä asennusta. Prerelease-only-tuote näkyy esijulkaisuvalinnan kautta.
Virheellinen versionumero raportoidaan eikä keskeytä vertailua poikkeuksella
toisen tuotteen päivityksen keskellä. Version prerelease-suffix ei saa päästä
vakaan päivityksen ehdotukseksi ristiriitaisen katalogilipun takia.

## ModulesManage ja asennus

Näkymään lisätään Näytä esijulkaisut -valinta, oletuksena pois.
Vakaat versiot näkyvät normaalisti. Valinnan ollessa päällä näkyvät myös
esijulkaisuversiot selkeästi merkittyinä. Moduulille voi valita saatavilla olevan
asennettavan version. Valinta perustuu tallennettuihin versioriveihin ja niiden
tunnisteisiin, ei asiakkaan itse antamaan URLiin. Suodatus tapahtuu ennen
listauksen total/paging-laskentaa, eikä prerequisite-only-tuote häviä väärän
AvailableVersion-suodattimen vuoksi.

Install-pyyntö välittää valitun version tunnisteen. Palvelin tarkistaa version
kuulumisen tuotteelle ja että sen paketti on ladattavissa, ja käyttää nykyistä
ZIP-asennus-/yhteensopivuustarkistusketjua sekä moduulin expectedId-tarkistusta.
Tyhjä valinta tarkoittaa uusinta sopivaa vakaata versiota. Esijulkaisu asennetaan
vain nimenomaisella version valinnalla. Asiakkaan tietoja ei käytetä vaihtamaan
paketin URLia tai luomaan uusia versiorivejä asennuksessa.

GXTable, nykyiset oikeudet ja progress/cancellation-käytännöt säilytetään.
Kanava-/suodatinvaihto nollaa sivutuksen. Virhe näytetään luettavana ja valittu
versio säilyy, jos lataus tai asennus epäonnistuu.

## Testaus ja hyväksymiskriteerit

Fake HTTP-katalogi testaa yhden latauksen per kierros, tuotetyyppien ja Id:n
mappauksen, useat agenttituotteet, vakaat/prerelease-versiot, asset-valinnan,
virheellisen/pakettia vailla olevan julkaisun, katalogivirheen ja cancellationin.
Erillinen SQLite-integraatio käyttää todellisia repositoryjä ja varmistaa
upsertin, metadata-päivityksen, rinnakkaisuuden ja aiemman datan säilymisen.
Versionvertailu testataan prerelease-numeroin, eri core-versioin ja assembly-version
neljännellä nollalla. Ei .Wait()/.Result-kutsuja uuden async-ketjun sisällä.

Todellinen listaus-/asennus-API ja Razor-testit kattavat esijulkaisuvalinnan,
prerelease-only-tuotteen, version valinnan, väärän tuotteen version eston ja
palvelimen tallennettuun asset-URLiin perustuvan asennuksen. Host build ja nykyiset
moduuli-/policy-/ZIP-testit säilyvät läpäisevinä. Live GitHub -julkaisua ei
käytetä pakollisena testinä, eikä käyttäjän tietokantaa muuteta testauksessa.

Valmis ratkaisu tallentaa katalogin moduuli-/agenttiversiot Gurux.Updater.Netillä
ja tarjoaa esijulkaisut hallitusti ModulesManageen ilman vanhojen syötteiden tukea.
