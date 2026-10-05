# Gurux.AMI.Invitations

## Tavoite ja hyväksytty toimintamalli

Siirretään kaikki Invite-toiminnot AMI:n Shared-, server- ja client-projekteista
addiniin `Gurux.AMI.Invitations`. Moduulin ID on `Invitations`, ja projekti
sijoitetaan hakemistoon
`C:/Projects/Gurux.DLMS.AMI4/Gurux.DLMS.AMI.Modules/Gurux.AMI.Invitations`.
Publish muodostaa julkaistavista tiedostoista `Invitations.zip`-asennuspaketin.
Paikallinen kehitys käyttää olemassa olevaa kehitysmoduulien latausta.

Moduuli luo `invitations.send`-policyn. Sen oletusvaatimus on rekisteröitynyt,
tunnistautunut käyttäjä käyttäen AMI:n nykyisiä policy- ja roolivakioita.
Policy on välttämätön mutta ei yksin riittävä oikeus kutsun lähettämiseen:
kutsuja saa kutsua vain ryhmään, jonka aktiivinen jäsen hän itse on.
Admin voi kutsua kaikkiin aktiivisiin ryhmiin. Tarkistukset tehdään palvelimella
tietokannan nykytilasta; asiakas ei voi ohittaa niitä ryhmätunnisteella.

Kutsu toimitetaan vastaanottajan sähköpostiin. Kutsun hyväksyminen edellyttää
kirjautumista käyttäjänä, jonka vahvistettu sähköposti vastaa kutsun vastaanottajaa.
Vastaanottaja voi rekisteröityä AMI:n nykyisellä rekisteröitymistoiminnolla ennen
hyväksymistä. Moduuli ei luo uusia käyttäjätilejä eikä ohita sähköpostin vahvistusta.

## Nykytila ja rajaus

Nykyiset Invite-DTO:t, InvitesController, InviteStore, InMemoryInviteStore,
GroupInvite, TokenGenerator ja Invite.razor siirretään tai korvataan moduulissa.
Nykyinen controller käyttää demo-API-avainta, ja kutsut sekä jäsenyydet ovat vain
muistissa. Uusi toteutus käyttää AMI:n autentikointia, policyjä ja todellista
`GXUserGroupUser`-jäsenyyttä. Demoavainta tai vanhoja endpointteja ei säilytetä.
Hostin Invite-tyyppien skeemapoikkeukset ja rekisteröinnit poistetaan.
Yhteiset käyttäjät, ryhmät, jäsenyydet ja sähköpostirajapinta jäävät hostiin.
Host ei viittaa moduuliprojektiin tai sen konkreettisiin tyyppeihin.

## Arkkitehtuuri

Käytetään nykyistä IAmiModule-runtimea, module-owned DI-scopeja, endpointteja,
policyllä suodatettuja sivu- ja tabirekisteröintejä sekä IAmiModuleServices-dispatcheria.
Erillistä lataajaa tai controller-discovery-mekanismia ei lisätä.
Vaihtoehto pelkästä muistivaraston siirrosta hylätään: se ei lisää todellista
ryhmäjäsenyyttä eikä säilytä kutsuja restartissa. Host-controllerin jättäminen
paikoilleen hylätään, koska Invite-kokonaisuus kuuluu poistettavalle addinille.

Moduuli omistaa kutsutaulun, tallennuspalvelun, API-mallit, käyttöliittymän,
sähköpostisisällön ja policyjen elinkaaren. Kutsusta tallennetaan tunniste,
tokenin tiiviste, ryhmätunniste, kutsujan käyttäjätunniste, vastaanottajan
normalisoitu sähköposti, luonti- ja vanhenemisajat sekä toimitus- ja käyttötila.
Raakatokenia ei tallenneta tietokantaan eikä lokiteta. Token on kryptografisesti
satunnainen ja URL-turvallinen. Voimassaolo on 1–24 tuntia, oletuksena 24 tuntia.

## Sähköpostiriippuvuus ja elinkaari

InstallAsync tarkistaa aktiivisen IAmiEmailSender-viennin saatavuuden ennen
skeeman tai oletuspolicyn muuttamista. Pelkkä hostin sähköpostiadapterin
DI-rekisteröinti ei todista toimivan lähettäjämoduulin saatavuutta.
Tarkistus ei lähetä koesähköpostia. Tarvittaessa runtimeen lisätään yleinen
viennin saatavuuskysely, joka ei paljasta moduulin toteutusinstanssia.
Puuttuva palvelu aiheuttaa selkeän asennusvirheen ilman osittaista policyasennusta.
Saatavuus tarkoittaa aktiivista palveluntarjoajaa; SMTP-tunnusten ja verkon
toiminta todetaan varsinaisessa lähetyksessä, ei luvata asennustarkistuksella.

StartAsync varmistaa skeeman ja oletuspolicyn myös kehityslatauksessa, joka
ohittaa InstallAsync:n. Lähettäjä voi aktivoitua moduulien käynnistysjärjestyksessä
myöhemmin; jokainen lähetys tarkistaa saatavuuden dispatcherilla uudelleen.
Sähköpostipalvelun katoaminen estää uudet lähetykset selkeällä virheellä.
Jo toimitettujen kutsujen hyväksyminen ei tarvitse sähköpostipalvelua.

UninstallAsync poistaa vain moduulin omistaman muuttamattoman oletuspolicyn.
Adminin muuttamaa policya ei poisteta. Omistustieto mahdollistaa samojen
policytunnisteiden uudelleenkäytön uudelleenasennuksessa.
Kutsudata ja syntyneet ryhmäjäsenyydet säilyvät poistossa. Sivut, tabit ja
endpointit poistuvat runtimen rekisteröintien mukana. StopAsync peruuttaa
käynnissä olevat operaatiot nykyisen runtimen cancellation-mallilla.

## Lähetys ja hyväksyminen

Palvelin listaa kutsujalle vain kutsuttavissa olevat ryhmät. Lähetys tarkistaa
policyn, aktiivisen ryhmän ja kutsujan jäsenyyden tai Admin-roolin uudelleen.
Vastaanottajan osoite validoidaan. Linkin alkuperä tulee luotetusta sovelluksen
julkisen osoitteen asetuksesta, ei tarkistamattomasta Host-headerista.
Sähköposti sisältää HTML-enkoodatut ryhmän ja kutsujan tiedot sekä hyväksymislinkin.

Kutsu tallennetaan ensin toimitusta odottavana. IAmiEmailSenderia käytetään vain
dispatcher-callbackin aikana, ja cancellation välitetään lähetykseen.
Onnistuneen lähetyksen jälkeen kutsu merkitään toimitetuksi. Epäonnistuneen tai
peruutetun lähetyksen kutsu ei ole hyväksyttävissä. SMTP:n ja tietokannan välillä
ei ole yhteistä transaktiota: jos sähköposti lähtee mutta toimitustilan tallennus
epäonnistuu, käyttäjä saa virheen ja linkki jää käyttökelvottomaksi.

Linkin avaaminen ei muuta jäsenyyttä. Kirjautunut vastaanottaja vahvistaa
liittymisen POST-operaatiolla. Palvelin tarkistaa tokenin, toimitustilan,
vanhenemisen, kertakäyttöisyyden, vahvistetun sähköpostin sekä ryhmän aktiivisuuden.
Se tarkistaa myös kutsujan nykyisen kutsupolicyn ja ryhmäoikeuden: poistuneen
jäsenyyden tai kutsuoikeuden jälkeen aiempi kutsu ei enää myönnä jäsenyyttä.
Jäsenyys lisätään tai aiempi poistettu jäsenyys palautetaan ja kutsu merkitään
käytetyksi samassa tietokantatapahtumassa. Jo aktiivinen jäsen ei saa duplikaattia.
Rinnakkaiset hyväksynnät eivät saa käyttää samaa kutsua kahdesti.

## Käyttöliittymä ja virheet

User-alueelle rekisteröidään policyllä suojattu Invitations-tab: ryhmävalitsin,
vastaanottajan sähköposti, voimassaolo ja lähetyspainike. Käytetään nykyisiä
Gurux.UI.Components-komponentteja, GXMenua toimintoihin ja GXTablea listauksiin.
Tabi on moduulin yhteinen policyllä näkyvä rekisteröinti, ei käyttäjäkohtainen rivi.
Hyväksymissivu käyttää yleistä /addin-reittiä; kirjautumisohjaus säilyttää
paluuosoitteen. Linkin tietoja ei paljasteta väärälle käyttäjälle.
IGXProgress näyttää lähetyksen etenemisen ja sallii peruutuksen.
API palauttaa luettavan ProblemDetails-virheen validoinnista, puuttuvasta
sähköpostipalvelusta, lähetysongelmista ja virheellisistä kutsuista.

## Julkaisu, testaus ja hyväksymiskriteerit

Publish ZIP sisältää moduulin assemblyn, riippuvuudet, PDB:t ja sisältötiedostot
nykyisen moduulipaketin käytännön mukaisesti. Yhteiset rajapinta-assemblyt jaetaan
hostin kanssa nykyisellä lataajalla. Dokumentoidaan kehitysasetukset,
sähköpostimoduulin aktivointi, Publish sekä kutsun lähetys- ja hyväksymiskulku.

Testit käyttävät erillistä testitietokantaa ja sähköpostipalvelun testivientiä.
Todellisella runtime-lataajalla testataan policy, sivumetadata, endpointit ja
ZIP-asennus. Testataan puuttuva sähköpostipalvelu InstallAsync:ssa, oman ryhmän
kutsu, vieraan ryhmän esto, Admin-poikkeus, policyllä estetty käyttäjä,
sähköpostin toimitus/peruutus/virhe, väärä ja vahvistamaton vastaanottaja,
vanheneminen, uudelleenkäyttö, rinnakkaisuus, restartin yli säilyvä kutsu,
todellinen jäsenyys, kutsujan menetetty oikeus sekä uninstallin vaikutukset.
Hostiin ei jää konkreettisia Invite-tyyppejä. Host build ja nykyiset
moduuli-/policytestit varmistavat muiden moduulien toiminnan säilymisen.

Hyväksymiskriteeri: rekisteröitynyt käyttäjä voi lähettää sähköpostikutsun oman
ryhmänsä jäseneksi, vastaanottaja voi turvallisesti hyväksyä sen, Admin voi kutsua
kaikkiin ryhmiin ja Invitations on itsenäisesti asennettava ja poistettava ZIP-addin.
