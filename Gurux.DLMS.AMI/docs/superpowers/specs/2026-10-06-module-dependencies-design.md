# Moduulien riippuvuudet ja yhteensopivuustarkistus

Päivä: 2026-10-06
Tila: käyttäjän tarkistettavana; ei vielä toteutusta.

## Tavoite ja hyväksytyt säännöt

Moduuli ilmoittaa AMI:lle tarvitsemansa isännän kirjastot ja muut AMI-moduulit.
Puuttuva tai väärän version riippuvuus estää asennuksen ja käynnistyksen.
AMI-moduulin riippuvuuden pitää olla aktiivinen. Sen sammuttaminen ja poistaminen
estetään, kun riippuvaisia moduuleja on aktiivisena. Virhe luettelee estävät
moduulit. Samat säännöt koskevat ZIP-asennusta ja kehityslatausta.

Tarkistetaan myös päivitys, uudelleenaktivointi ja koko palvelimen käynnistys.
Riippuvuuksia ei asenneta eikä riippuvaisia moduuleja sammuteta automaattisesti.
Nykyistä jaettujen rajapinta-assemblyjen käyttöä ja ZIP-paketointia jatketaan.

## Nykyinen toteutus

PluginLoadContext.ValidateDependencies tarkistaa assemblyjen löytymisen.
Jaetut assemblyt ratkaistaan nimen perusteella isännästä; yhteensopivaa versiota
ja aktiivisten AMI-moduulien riippuvuuksia ei tarkisteta.

GXModuleService.InstallCandidateAsync tarkastaa paketin, sammuttaa edellisen
version ja muuttaa pakettihakemistoa ennen InstallAsync/StartAsync-kutsuja.
Riippuvuuksien ennakkotarkistus pitää sijoittaa ennen näitä muutoksia.
GXModuleRuntime omistaa aktiiviset moduulit, palveluviennit ja kutsujen elinkaaren.
StartConfiguredModulesAsync lataa kehitysmoduulit ennen asennettuja moduuleja;
nykyinen järjestys pitää korvata riippuvuuksien mukaisella yhteisellä järjestyksellä.
GXModuleStartupService ja runtime.DisposeAsync sammuttavat nykyisin moduulit
listajärjestyksessä. Sammutusjärjestys pitää kääntää riippuvuuksien vastaiseksi.

## Valittu ratkaisu

Suositus: yhteinen riippuvuuskuvaus moduulirajapintaan sekä runtimeen yksi
riippuvuusgraafi. Pakettitarkistus, latausjärjestys ja pysäytys käyttävät samaa
mallia. Tietokannan Active-lippu ei yksin todista, että riippuvuus on käytettävissä.

Pelkkä assemblyviittausten tarkistus ei tunnista ajonaikaisia moduulipalveluja.
Pelkkä asennuksen tarkistus puolestaan sallisi riippuvuuden sammuttamisen myöhemmin.
Näistä syistä molemmat tarkistukset yhdistetään runtimegraafiin.

IAmiModule saa Dependencies-ominaisuuden. GXAmiModuleBase palauttaa oletuksena
tyhjän listan. Kuvaus sisältää Kind-, Id-, MinimumVersion- ja
MaximumVersionExclusive-kentät. Rajat tarkoittavat vähintään minimiversiota ja
alle maksimiversion. Tyhjä yläraja sallii uudemmat versiot. Molemmat rajat voivat
olla tyhjiä, jolloin vaaditaan olemassaolo ja aktiivisuus ilman version rajausta.
Virheelliset versiot, ristiriitaiset rajat ja itseensä viittaavat moduulit hylätään.

Riippuvuustyypit:

- HostAssembly: assemblyn yksinkertainen nimi, esimerkiksi Gurux.DLMS.AMI.Shared
  tai Gurux.UI.Components. Se pitää löytyä isännän jaetuista assemblyistä.
  Pakettiin sisältyvä toinen kopio ei täytä tätä riippuvuutta.
- Module: IAmiModule.Id, esimerkiksi SMTP-moduulin todellinen tunniste.
  Nimi, tiedostonimi ja käyttöliittymän otsikko eivät korvaa tunnistetta.
- Service: jaetun palvelurajapinnan täysi tyyppinimi. Riippuvuus ratkaistaan
  aktiivisen viennin omistavaan moduuliin. Tämä säilyttää Invitations-moduulin
  nykyisen mahdollisuuden käyttää mitä tahansa IAmiEmailSender-toteutusta.
  Palveluntarjoajaa ei vaihdeta aktiivisen kuluttajan alta.

Moduuli- ja assemblytunnisteet vertaillaan kirjainkoosta riippumatta;
palvelurajapinnan nimi ratkaistaan isännän jakamasta sopimustyypistä.
Kuvaukset kopioidaan isännän omiksi arvoiksi. Manifesti sisältää riippuvuudet
ja niiden ratkaistut moduulitunnisteet, ei pluginin Type- tai delegaattiviittauksia.

## Versiot ja kirjastojen tarkistus

Eksplisiittiset versiorajat käyttävät AssemblyInformationalVersion-arvoa ilman
build-metatietoa; puuttuvan tiedon varalla käytetään AssemblyVersion-arvoa.
Samaa lähdettä käytetään sekä isännän kirjastoille että aktiivisille moduuleille.
Manifestiin lisätään erillinen CompatibilityVersion. Nykyinen tiedosto-/assembly-
versio ja siihen perustuvat pakettihakemistot säilyvät. Katalogin ReleaseVersion
on julkaisun tunniste eikä korvaa koodin yhteensopivuusversiota.

Semanttinen vertailu tukee nykyisiä 2–4-osaisia numeroversioita ja esijulkaisuja.
Build-metatieto ei vaikuta vertailuun. Vakaata versiota uudemman minimin vaativaan
riippuvuuteen ei hyväksytä saman version esijulkaisua. Esijulkaisu kelpaa vain,
jos ilmoitettu versioalue sen kattaa. Virheellistä versiota ei tulkita nollaksi.

Jaettujen assemblyjen viittaukset tarkistetaan lisäksi automaattisesti ennen
moduulin koodin lataamista: tarjotun AssemblyVersionin pitää olla vähintään
viitattu versio ja major-version pitää vastata. Tämä tarkistus käyttää assemblyn
viittausmetadataa, koska viittaukset eivät sisällä InformationalVersionia.
Eksplisiittiset HostAssembly-riippuvuudet voivat asettaa tätä tarkemmat rajat.
Lataaja ei korvaa epäyhteensopivaa isännän sopimusta paketin omalla kopiolla.
Yksityiset DLL-riippuvuudet säilyvät AssemblyDependencyResolverin ratkaistavina.

Moduulien rakentajat ja Dependencies-getterit ovat sivuvaikutuksettomia.
Metadata tarkastetaan ennen Configure/InstallAsync/StartAsync-kutsuja. Tämä ei
ole turvaraja epäluotettavan plugin-koodin suorittamiselle; pakettien nykyinen
luottamusmalli säilyy. Rajapinnan muutos edellyttää moduulien uudelleenkäännöstä;
erillistä vanhan moduulirajapinnan sovitinta ei toteuteta.

## Asennus, käynnistys ja samanaikaisuus

ZIP puretaan tarkastusta varten vain tilapäishakemistoon. Ehdokkaan tunniste,
kirjastoviittaukset, riippuvuudet ja yhteensopivuus tarkistetaan ennen vanhan
moduulin pysäyttämistä, hallitun hakemiston muutoksia tai tietokantakirjoituksia.
Tilapäiset tarkastusresurssit vapautetaan myös virheessä.

Runtime tarkistaa riippuvuudet uudelleen ja varaa ratkaistut toimittajat ennen
elinkaarikutsuja. Varaus estää toimittajan pysäytyksen myös silloin, kun
riippuvainen moduuli on Loading-tilassa tai sen Stopping-kutsuja vielä tyhjennetään.
Varaus poistetaan vasta epäonnistuneen aktivoinnin siivouksen tai täydellisen
pysäytyksen jälkeen. Riippuvuus saa olla vain Active-tilassa aktivoinnin alussa.

Graafin tarkistus ja varausten muutos ovat atomisia. Per-moduuli-portit säilyvät.
Elinkaarikutsuja ja käynnissä olevien kutsujen tyhjentämistä ei suoriteta graafin
lukon sisällä. Samanaikainen aktivointi/pysäytys ei saa ohittaa tarkistusta eikä
aiheuttaa riippuvuuslukkojen keskinäistä odotusta.

Käynnistyksessä tarkastetaan kehitys- ja asennetut ehdokkaat ensin yhdessä.
Kehityslähde voittaa saman tunnisteen asennetun paketin nykyisen valintakäytännön
mukaisesti. Graafi järjestetään topologisesti: toimittaja ensin, kuluttaja sen jälkeen.
Syklit ilmoitetaan tunnisteketjuna. Puuttuva, epäaktiiviseksi määritetty tai
epäyhteensopiva toimittaja estää kuluttajan käynnistymisen.
Riippumattomat kelvolliset moduulit voivat käynnistyä; virheellinen kuluttaja jää
pois runtimesta ja virhe kirjataan selvästi. Kehityslähteiden nykyinen fail-fast-
käytäntö säilyy. Aktiiviseksi merkitty asennettu riippuvuus käynnistetään ensin,
ei merkitä aktiiviseksi vain tietokantatietojen perusteella.

## Sammutus, poisto, päivitys ja palautus

StopAsync ja RemoveAsync tarkistavat riippuvaiset moduulit ennen manifestin,
reitityksen, peruutustokenien, tietokannan tai tiedostojen muutoksia. Sama koskee
GXModuleServicen disable-, delete- ja refresh-polkuja. Estetty toiminto säilyttää
moduulin, sen datan ja paketin ennallaan.

Toimittajan päivitys vaatii sen pysäyttämisen, joten päivitys estetään aktiivisten
kuluttajien aikana, myös yhteensopivaan versioon. Käyttäjä pysäyttää ensin
kuluttajat ja käynnistää ne uudelleen toimittajan päivityksen jälkeen.

Jos käyttäjä on nimenomaisesti valinnut useita moduuleja sammutettavaksi tai
poistettavaksi, valitut moduulit käsitellään kuluttaja ennen toimittajaa.
Valinnan ulkopuolisia riippuvaisia moduuleja ei sammuteta automaattisesti.
Koko AMI:n sammutus sulkee kaikki moduulit samassa käänteisessä järjestyksessä
ja säilyttää nykyisen peruutus-/tyhjennyskäytännön.

Epäonnistuneen asennuksen palautus käyttää samoja riippuvuussääntöjä ja vapauttaa
uuden ehdokkaan varaukset. Ennakkotarkistusvirhe ei koskaan sammuta vanhaa versiota.
Jos toimittaja on vielä pysähtymässä, kuluttajan aktivointi hylätään eikä sitä
kytketä keskeneräiseen toimittajaan.

Esimerkkivirheet:

- Moduuli X vaatii aktiivisen moduulin Y >= 1.2.0 ja < 2.0.0. Y ei ole aktiivinen.
- Moduuli X vaatii Gurux.UI.Components >= 1.2.0 ja < 2.0.0. Käytössä on 1.0.0.
- Moduulia Y ei voi sammuttaa tai poistaa. Riippuvaiset moduulit: X, Z.
- Moduulien riippuvuussykli: X -> Y -> X.

## Muutokset ja tarkistusnäyttö

Rajapinnan DTO:t ja Dependencies lisätään Gurux.DLMS.AMI.Module-projektiin.
Hostissa muutokset rajataan PluginLoadContextiin, yhteiseen riippuvuustarkistimeen,
GXModuleRuntimeen, GXModuleServiceen ja GXModuleStartupServicen sammutukseen.
Moduulimanifestin tiedot välitetään nykyisissä moduulien API-vastauksissa.
Moduulinhallinta näyttää nykyisellä virhekäsittelyllä selkeät estoperusteet.

Nykyiset moduulit ja testifixturet käännetään uudelleen. Pakollista sähköpostia
käyttävä Invitations ilmoittaa Service-riippuvuuden IAmiEmailSenderiin.
IpAccessControlin valinnainen sähköposti ei luo pakollista riippuvuutta.
Yhteisten kirjastojen automaattiset tarkistukset kattavat kaikki moduulit.
Kehittäjän ohje kuvaa ilmoitukset, versiolähteen ja oikean pysäytysjärjestyksen.

Testit käyttävät oikeaa runtimea, erillistä SQLite-tietokantaa ja fixturepaketteja.
Todennetaan vähintään:

1. Puuttuva, epäaktiivinen ja väärän version moduuli estävät asennuksen ennen
   InstallAsync-kutsua ja pysyviä muutoksia. Vanha aktiivinen versio säilyy.
2. Oikean version aktiivinen moduuli ja yhteensopivat isännän kirjastot hyväksytään.
3. Jaetun assemblyn vanhempi tai väärän major-version kopio ei läpäise tarkistusta.
4. ZIP- ja kehityslataus käyttävät samoja tarkistuksia; yhteiset sopimukset tulevat
   isännästä, yksityiset riippuvuudet paketista.
5. Kehitys-/asennettujen moduulien sekoitettu ketju käynnistyy oikein, syklit ja
   virheelliset versiorajat hylätään, esijulkaisujen vertailu toimii.
6. Riippuvuuden stop/remove/update estyy ja jättää tilan/data-/pakettitiedot ennalleen.
7. Kuluttajan poistamisen jälkeen toimittaja voidaan pysäyttää/poistaa.
8. Palveluvienti ratkaistaan oikeaan aktiiviseen omistajaan ja se suojaa omistajaa.
9. Samanaikainen aktivointi ja stop eivät riko varausta; peruutus ja rollback
   vapauttavat varaukset oikeassa vaiheessa.
10. Koko AMI:n sammutus, valittu monimoduulipoisto ja käynnissä olevien kutsujen
    peruutus toimivat kuluttaja ensin -järjestyksessä.

Lopuksi ajetaan moduulien elinkaaren ja nykyisten addinien regressiotestit sekä
hostin build. Oikeita tuotantotietokantoja tai julkaistuja paketteja ei muuteta.
