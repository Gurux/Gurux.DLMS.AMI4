# Gurux.AMI.IpAccessControl

## Tavoite ja hyväksytty rajaus

Siirretään nykyinen IpAccessControl itsenäiseen addiniin hakemistoon
`C:/Projects/Gurux.DLMS.AMI4/Gurux.DLMS.AMI.Modules/Gurux.AMI.IpAccessControl`.
Projektin ja assemblyn nimi on `Gurux.AMI.IpAccessControl`; moduulin ID on
`IpAccessControl`. Moduuli käyttää olemassa olevaa runtime-lataajaa sekä ZIP-
että kehitysasennuksessa. Legacy-toteutusta ei ylläpidetä rinnalla.

Seurataan ainoastaan AMI:n vastaanottamia HTTP/HTTPS-pyyntöjä. Mukana ovat
kirjautumattomat pyynnöt; pelkkiä TCP-yhteyksiä ei seurata. MAC-osoitetta ei
saada HTTP-pyynnöstä eikä sitä käytetä tunnistukseen tai pääsynhallintaan.

## Nykyinen toteutus ja muutettavat rajat

Nykyinen `BlockService` yhdistää IP-sääntöjen tarkistuksen, kirjautumisen
kirjauksen ja keskeneräisen sähköposti-ilmoituksen. Sitä kutsuvat
`GXApplicationSignInManager`, `GXApplicationUserManager` ja
`GXCookieAuthenticationEvents`. UI ja valikkolinkit on kiinnitetty hostiin
`UserTab.razor`, `Configurations.razor`, `ServerSettings` ja `MenuRepository`.
Runtime tukee HTTP-middlewarea ja endpointteja, mutta ei vielä policyllä
suojattuja tabideklarointeja.

Suositeltu toteutus käyttää moduulin middlewarea HTTP-havainnointiin ja
yleistä moduulin tabideklarointia käyttöliittymän liittämiseen. Vaihtoehto,
jossa vanha hostin UI ja BlockService jäävät varsinaiseksi toteutukseksi,
ei täytä itsenäisen addinin tavoitetta. Palvelinkohtainen TCP-hook ei kuulu
korjattuun HTTP-rajaukseen.

## Moduulin ja hostin vastuut

Addin omistaa IP-käsittelyn palvelun, rajapinnat, hallintakomponentin,
käyttäjän IP-listan ja ilmoitusten lähettämisen. Hostiin jää vain yleinen
moduulien tabien/renderöinnin tuki sekä kirjautumisvirran käyttämä
moduulipalvelun kutsurajapinta, jos kirjautumisen eston ajoitus sitä vaatii.
Host ei viittaa konkreettiseen addin-assemblyyn.

Käytetään hostin ja addinin yhteisiä DTO-/ORM-/sopimusassemblyjä.
Olemassa olevat `GXIpAddress`-tiedot säilyvät; niitä ei poisteta siirrossa.
Asennus varmistaa tarvittavat skeemat ja policyt idempotentisti.
Kehityslatauksen StartAsync varmistaa samat edellytykset, sillä se ei kutsu
InstallAsync-hookia.

## Havainnot ja pääsynhallinta

IP luetaan `HttpContext.Connection.RemoteIpAddress`-arvosta, normalisoidaan
IPv4-mapped IPv6 mukaan ja tallennetaan omana havaintonaan. Proxyjen
osoitteiden käsittely käyttää hostin luotettujen proxyjen asetuksia;
moduuli ei luota asiakkaan itse lähettämään X-Forwarded-For-headeriin.

Kirjautumattoman pyynnön havainto ei kuulu käyttäjälle. Kun käyttäjä on
tunnistettu, havainto liitetään nykyiseen käyttäjätunnisteeseen. Listat
näyttävät IP:n, käyttäjän kun sallittu, ensimmäisen havainnon,
viimeisimmän havainnon ja säännön tilan. Käyttäjä/IP-yhdistelmän ensimmäinen
havainto tunnistetaan atomisesti, jotta rinnakkaiset pyynnöt eivät aiheuta
päällekkäisiä rivejä tai ilmoituksia.

Pääsynhallinnan säännöt erotetaan havainnoista. Adminin lisäämä sääntö on
globaali eikä se anna käyttäjälle omistajuutta havaintoon. Block estää
osoitteesta tulevat pyynnöt HTTP 403 -vastauksella. Allow sallii osoitteen,
mutta yksittäisen Allow-säännön lisääminen ei automaattisesti estä kaikkia
muita osoitteita. Admin voi erikseen ottaa käyttöön whitelist-only-tilan.
Block voittaa ristiriitaisen Allow-säännön. Muutokset tulevat voimaan
heti. Moduulin poiskytkeminen lopettaa havainnoinnin ja sen estot.

Middleware käsittelee HTTP-pyynnöt myös anonyymeille; kirjautumisen
epäonnistuminen ei saa johtaa käyttäjäkohtaisen uuden IP:n ilmoitukseen.
Kirjautuminen ja uudistetun session käyttö estetään, kun IP on blokattu.

## Policyt, tabit ja Config

Moduulimalliin lisätään yleinen tabideklarointi: kohdealue, otsikko,
komponentin tyyppikuvaus, vaadittu policy ja moduulin tunniste.
Kohdealueet ovat User ja Config. Tämä ei ole käyttäjäkohtainen tabirekisteri.
Host renderöi aktiivisten moduulien deklaroinnit ja tarkistaa policyn.

`ipaccesscontrol.view` edellyttää authenticated-käyttäjää ja avaa User-tabin.
`ipaccesscontrol.manage` edellyttää authenticated-käyttäjää ja Admin-roolia
ja avaa Config-hallinnan. Ne tallennetaan GXPolicy/GXPolicyRequirement-
malliin ja käyttävät nykyistä policy-evaluointia. Admin voi muuttaa
view-policyn vaatimuksia nykyisestä policyhallinnasta.

API tarkistaa samat policyt palvelimella. Tavallisen käyttäjän listaus
rajataan aina palvelimen tunnistamaan käyttäjään; asiakas ei voi antaa
toisen käyttäjän ID:tä rajauksen ohittamiseksi. Admin saa kaikki havainnot,
myös kirjautumattomat. Hallinnan add/block/allow-operaatiot ovat vain
Adminille. Moduuliassetien ja manifestien lukeminen sallitaan tabipolicyn
perusteella ilman vaatimusta yleisestä module.view-hallintaoikeudesta;
palvelin palauttaa vain ne moduulit, joihin käyttäjällä on oikeus.

Addinin UI käyttää GXTablea ja suodattimia. GXMenu/nykyinen menu-komponentti
tarjoaa Adminille Lisää, Blokkaa ja Whitelist-toiminnot. Käyttäjän oma lista
on vain luettavissa. Aktiivisen moduulin tabit näkyvät asennuksen jälkeen;
deaktivointi tai poisto poistaa deklaroinnit ja näkyvät tabit myös jo
avatuista näkymistä runtime-muutosilmoitusten kautta. Vanha kiinteä User-tab,
Config-haara ja hostin IpAccessControl-linkit poistetaan.

## Sähköposti uudesta IP-osoitteesta

Ensimmäinen onnistuneesti tunnistettu havainto uudesta käyttäjä/IP-parista
lähettää käyttäjän tallennettuun sähköpostiosoitteeseen ilmoituksen,
jos aktiivinen IAmiEmailSender-export löytyy. Kutsutaan sitä hostin
IAmiModuleServices-dispatcherin kautta; addin ei säilytä provider-viitettä.
Jos provider puuttuu, IP-havainto säilyy ja pyyntö jatkuu normaalisti.
Jos käyttäjällä ei ole sähköpostiosoitetta, lähetystä ei tehdä.
Lähetysvirhe kirjataan lokiin eikä se estä käyttäjän HTTP-pyyntöä.
Moduulin pysäyttäminen peruuttaa keskeneräisen ilmoitustyön. Ilmoitusten
luotettava toimitusjono ja uudelleenyritykset eivät kuulu tähän muutokseen.

## Poisto ja tietojen säilyttäminen

Uninstall poistaa moduulin omat UI-deklaroinnit/valikkorekisteröinnit ja
mahdolliset sen luomat omistajuudella merkityt policyt; muiden muokkaamia
tai jakamia policyjä ei poisteta perusteetta. IP-historiaa ja sääntöjä ei
poisteta automaattisesti moduulin uninstallissa. Historiallisten tietojen
säilyminen ei saa jättää estologiikkaa aktiiviseksi ilman moduulia.

## Virheet ja testit

Syötteiden IP-validointi ja virheet palautetaan ProblemDetails-muodossa.
Käyttäjän operaatiot käyttävät IGXProgressia ja peruutustokenia.
MAC-kenttää ei näytetä, koska sitä ei voida täyttää luotettavasti.

Testit käyttävät todellista moduuliruntimea ja erillistä SQLite-testikantaa:
anonyymi havainto, käyttäjän oma rajaus, Adminin kaikki havainnot,
rinnakkaisten ensihavaintojen deduplikointi, IPv4/IPv6-normalisointi,
Block/Allow/whitelist-only, uuden IP:n sähköposti, puuttuva provider,
lähetysvirhe, policy-denial, tabin ilmestyminen ja poistuminen sekä
uudelleenasennus. Ei yhteyksiä sovelluksen normaaliin tietokantaan eikä
oikeiden sähköpostien lähetystä testien aikana.

Varmennetaan addin- ja host-build, olemassa olevat moduuli-/policytestit,
kehityslataus samassa shadow-copy-mallissa kuin SMTP ja ZIP-asennus.
