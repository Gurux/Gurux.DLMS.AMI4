# Moduulin datan ja taulujen poistaminen

## Hyväksytty toimintamalli

AMI:n moduulin poistodialogi kysyy aina poistamisen laajuuden:

1. Säilytä data ja taulut (oletus).
2. Poista moduulin data, säilytä taulut.
3. Poista moduulin data ja taulut.

Valinta välitetään poistopyynnössä palvelimelle ja nykyisen UninstallAsync-
metodin AmiModuleContext-oliossa moduulille. InstallAsync:n allekirjoitus ei muutu.
Pelkkä moduulin pysäyttäminen, päivittäminen tai epäonnistuneen latauksen siivous
ei poista dataa tai tauluja. Puuttuva valinta tarkoittaa säilyttämistä.

## Rajapinnat ja omistus

Yhteinen sopimus määrittelee enum-arvot Preserve, DeleteData ja DeleteDataAndTables
sekä immutable AmiModuleUninstallOptions-olion. AmiModuleContext.UninstallOptions
sisältää valinnan. Tuntemattomat enum-arvot hylätään palvelimella ennen poiston alkua.

AmiModuleBuilder.OwnTable<T>() ilmoittaa moduulin liiketoimintadataa sisältävän
taulun. Erillinen metadata-luokitus ilmoittaa moduulin omistus-/migraatiotaulut.
Data-only-valinta tyhjentää liiketoimintataulut; omistus-/migraatiometadata säilyy,
jotta uudelleenasennuksen policytunnisteet ja skeeman tila eivät katoa.
DataAndTables poistaa myös moduulin ilmoittamat metadatataulut.

Host ylläpitää pysyvää moduuli–tauluomistusrekisteriä. Omistustiedossa on moduuli-ID
ja tietokannan todellinen taulutunniste. Rekisteröinti on nimenomainen omistusilmoitus,
ei kaikkien assemblyn luokkien skannaus tai taulunimen prefixiin perustuva arvaus.
Toisen moduulin omistamaa taulua ei voi ottaa omistukseen. Hostin yhteisiä tauluja
kuten GXUser, GXUserGroup, GXUserGroupUser, GXPolicy ja GXModule ei voi ilmoittaa
moduulin omiksi tauluiksi. Shared/host-tyyppien vahingossa rekisteröinti estetään.
Siirrettyjen addinien nykyiset taulut voidaan rekisteröidä nimenomaisesti niiden
Configure-vaiheessa; olemassa olevaa dataa ei tällöin muuteta.

Tauluomistusrekisteri kuuluu hostille ja säilyy myös poistossa. Onnistunut
taulujen poisto merkitään rekisteriin; se ei anna muille moduuleille automaattista
oikeutta samaan tauluun. Tyypit ratkaistaan ladatusta moduulista poistamisen ajaksi;
host ei säilytä plugin-Type-olioita pitkäikäisessä rekisterissä.

## Poistoketju ja virheet

Nykyinen RemoveModule-pyyntö saa poistovalinnan. Controller ja GXModuleService
välittävät sen runtimeen. Samat vaihtoehdot toimivat aktiiviselle ja pysäytetylle
moduulille; pysäytetyn moduulin paketista luetaan Configure/Uninstall-tiedot
käynnistämättä moduulin liiketoimintatoimintoja.

Palvelin tarkistaa poistooikeuden, omistuksen ja tietokannan riippuvuudet ennen
tuhoavaa siivousta. Rekisteröimätöntä taulua ei koskaan poisteta. Puuttuva taulu
on idempotentisti jo poistettu. Poiston kohteet eivät tule asiakkaan antamista
taulunimistä; asiakas lähettää vain moduuli-ID:n ja laajuusvalinnan.

Moduulin endpointit, middleware, tabit ja viennit poistetaan, käynnissä oleville
kutsuille pyydetään peruutusta ja leasejen päättymistä odotetaan. Siivous tapahtuu
ennen moduulin palvelujen ja assembly-kontekstin vapauttamista.
UninstallAsync käsittelee moduulin omat rivit yhteisissä tauluissa sekä mahdolliset
tiedostot käyttäen valintaa. Host käsittelee rekisteröidyt omat taulut sen jälkeen.
Nykyisten policyjen omistus- ja muokkaussuojat säilyvät: käyttäjän muokkaamia
policyjä ei poisteta automaattisesti, eikä muiden käyttäjien yhteisiä rivejä poisteta.

Taulut tyhjennetään/poistetaan lapsi–vanhempi-riippuvuusjärjestyksessä. Tarkistus
huomioi todellisen tietokannan FK:t, ei vain moduulin uusimman mallin annotaatioita.
Ulkopuolinen viittaus tai cascade, joka voisi poistaa toisen moduulin/hostin rivejä,
estää automaattisen poiston selkeällä virheellä. Rajoitteita ei poisteta tai ohiteta.
Sykliset riippuvuudet tai providerin puuttuva turvallinen riippuvuustarkistus
hylätään ennen mutaatioita; niitä ei ratkaista arvaamalla.

Data-only-siivoaminen käyttää tietokantatransaktiota. DDL:n atomisuutta ei luvata
kaikilla tuetuilla providereilla: osittainen taulupoisto raportoidaan ja uudelleen-
yritys käsittelee jo poistetut taulut idempotentisti. Tiedostopoistoja ja moduulin
omia hook-operaatioita ei väitetä samaksi transaktioksi tietokantasiivouksen kanssa.

Jos UninstallAsync tai taulusiivous epäonnistuu, moduulin pakettia ja rekisteririviä
ei poisteta. Moduuli jää pysäytetyksi/virhetilaan, josta poistamista voi yrittää
uudelleen samalla tai säilyttävällä valinnalla. Nykyinen cleanup-lokitus ei saa
niellä poiston epäonnistumista niin, että controller silti poistaa paketin/rivin.
Stop-timeout ei käynnistä rinnakkaista toista poistoa tai muuta jo hyväksytyn
poisto-operaation valintaa. Kesken oleva poisto ilmoitetaan käyttäjälle.

## Käyttöliittymä

Moduulilistan ja yksittäisen moduulin poistaminen käyttävät samaa dialogia.
Monivalinnassa valittu laajuus koskee dialogissa lueteltuja moduuleja; onnistumiset
ja virheet raportoidaan moduulikohtaisesti, eikä epäonnistuneita piiloteta listasta.
Peruutus sulkee dialogin lähettämättä poistopyyntöä. Tuhoavat valinnat kertovat
selkeästi, että moduulin sisältö poistetaan. Taulupoisto kertoo myös skeeman poistosta.
Oletusvalinta on aina Preserve, ei edellisessä poistossa käytetty tuhoava valinta.

## Nykyisten moduulien integrointi

Proxy rekisteröi GXCluster/GXRoute/GXDestination-taulut. IpAccessControl rekisteröi
oman IP-historian ja sääntöjen taulut. Invitations rekisteröi kutsutaulunsa,
ei yhteistä GXUserGroupUser-jäsenyystaulua. ContentManager rekisteröi omat
sisältö-/tyyppi-/kenttä-/ryhmä-/arvo-/käyttäjäasetustaulunsa. Moduulien omistus-
ja migraatiotaulut ilmoitetaan metadataksi. EndpointExplorer rekisteröi oman
policyomistusmetadatansa. SMTP:n rekisteröidyn liiketoimintataulun puuttuminen
ei ole virhe; moduuli voi edelleen toteuttaa oman UninstallAsync-siivoamisen.

Kunkin moduulin hook tarkistaa valinnan vain sille kuuluvissa yhteisissä riveissä
ja tiedostoissa. Esimerkiksi Invitationsin poistovalinta ei poista jo syntyneitä
käyttäjäryhmäjäsenyyksiä, koska ne ovat yhteistä käyttäjäryhmädataa.

## Testaus ja hyväksymiskriteerit

Testataan dialogin kolme valintaa, Preserve-oletus, peruutus ja monivalinnan
moduulikohtaiset virheet. Todelliset API/runtime/SQLite-testit kattavat aktiivisen
ja pysäytetyn moduulin, valinnan näkymisen hookissa, Preserve/data-only/drop,
FK-järjestyksen, ulkopuolisen viittauksen eston, omistusristiriidan, puuttuvan
taulun, epäonnistuneen hookin, osittaisen siivouksen uudelleenyrityksen ja timeoutin.
Testeissä ei käytetä käyttäjän tietokantaa tai asennettuja tuotantomoduuleja.

Nykyisten addinien omistustaulut tarkistetaan käytännön lifecycle-/ZIP-testeillä.
Host build ja nykyiset runtime-/policy-/moduulitestit säilyvät läpäisevinä.
Tuetuille tietokantaprovide­reille riippuvuustarkistus testataan käytettävissä
olevissa erillisissä testipalveluissa; testaamattomat tai tukemattomat tuhoavat
poistot hylätään turvallisesti ja niiden rajoitus dokumentoidaan.

Valmis toiminto kysyy poiston laajuuden ja poistaa vain nimenomaisesti moduulin
omistamaksi ilmoitettua dataa/tauluja. InstallAsync:n allekirjoitus säilyy.
