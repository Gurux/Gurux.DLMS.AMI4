# Gurux.AMI.Proxy

## Hyväksytty tavoite

Siirretään Proxy-toiminnot AMI:n Shared-, server- ja client-projekteista addiniin
`Gurux.AMI.Proxy`, ID `Proxy`. Projektipolku on
`C:/Projects/Gurux.DLMS.AMI4/Gurux.DLMS.AMI.Modules/Gurux.AMI.Proxy`.
Publish muodostaa julkaisutuloksesta `bin/<Configuration>/Proxy.zip`-paketin.
Nykyinen proxydata säilyy. Config-alueen proxyhallinta on oletuksena vain adminille.
Proxyliikenteen nykyinen julkisuus säilyy; hallintapolicy ei suojaa välitettyjä pyyntöjä.

## Nykytila ja siirrettävä kokonaisuus

Host rekisteröi YARP-palvelut, GXDbProxyConfigProviderin, GXProxyConfigReloaderin
ja MapReverseProxy-reitit. Shared omistaa GXCluster-, GXRoute- ja GXDestination-
taulut sekä LoadBalancingPolicy- ja TransformPathPattern-enumit. Nykyinen
Proxy.razor on Cron-asetuksista kopioitu näkymä eikä todellinen proxyeditori.
Reloaderin ajastettu toiminta on kommentoitu pois.

Addin omistaa mallit, enumit, skeeman, repositoryt, YARP-asetuslähteen,
välityksen palvelut, hallinta-API:n, Razor-editorit ja oman policyn.
Hostista poistetaan konkreettiset proxytyypit, YARP-rekisteröinti ja pakettiviite,
kiinteä Proxy-konfiguraation UI-seedaus/reitti sekä proxytaulujen luonti/poisto.
Vanhat taulunimet, sarakkeet, tunnisteet ja suhteet säilytetään uudessa moduulissa.
Taulujen alustaminen kuuluu InstallAsync/StartAsync:lle.
Vanhaa Cron-näkymää ei ylläpidetä legacy-reittinä.

## Runtime ja YARP

Säilytetään YARP:n varsinainen reverse proxy -pipeline, reittien valinta,
kuormantasaus, session affinity ja path-transformit. Pelkkä käsin tehty
HttpClient-välitys ei korvaa näitä ominaisuuksia. YARP:n palvelut ja asetuslähde
rekisteröidään moduulin omaan DI-konttiin; moduuli ei käynnistä omaa palvelinta.

Nykyinen AmiModuleBuilder.MapMethods tukee ennen käynnistystä määriteltyjä reittejä.
YARP tarvitsee muuttuvan EndpointDataSource-lähteen ja moduulin omat palvelut.
Runtimeen lisätään yleinen moduulin omistaman endpoint-lähteen rekisteröintituki.
Hostin yhdistetty reittirekisteri saa lähteen hostin omistaman välityskerrokseen
kautta. Se välittää reittimallin, metodit ja tarvittavan routing/authorization-
metadatan ja ohjaa suorituksen moduulin nykyiseen lease-/scope-/cancellation-malliin.
YARP-pipeline käyttää moduulin palveluntarjoajaa; HTTP-pyynnön alkuperäinen
host-palveluntarjoaja palautetaan suorituksen jälkeen.

Yleinen integraatio ei tunne GXClusteria, Proxy-moduulia tai YARP-rajapintoja.
Muiden moduulien nykyinen endpoint-rekisteröinti jatkaa toimintaansa.
Lähteen ChangeToken päivittää hostin reitit. Stop irrottaa lähteen ja subscriptionin
ennen palvelujen vapauttamista. Uusi pyyntö ei voi aloittaa välitystä pysäytettyyn
moduuliin. Käynnissä oleville välityksille pyydetään peruutusta ja runtime odottaa
leasejen päättymistä. Hostin lähteeseen tai serializer-välimuistiin ei jää moduulin
tyyppejä tai handler-delegaatteja sulkemisen jälkeen.

## Asetukset, tallennus ja käyttöönotto

Klusteri sisältää nykyisen kuormantasausasetuksen ja session affinity -valinnan.
Kohde sisältää tunnisteen, klusterin ja absoluuttisen HTTP(S)-osoitteen.
Reitti sisältää tunnisteen, klusterin, match-polun, path-transformin ja mahdollisen
custom-polun. Osoitteet ja reittimallit validoidaan palvelimella sekä YARP:n
konfiguraatiovalidoinnilla. Puuttuva klusteri, duplikaattitunniste, virheellinen
polku/enum/osoite tai viittaus poistettuun kohteeseen hylätään luettavalla virheellä.

Lue klusterit, kohteet ja reitit niin, ettei nykyinen inner join -ketju hukkaa
klusteria, jolla ei ole reittiä tai kohdetta. Poistettuja rivejä ei aktivoida.
Perusasetusten None/default-arvot tulkitaan yhdenmukaisesti vanhan toiminnan kanssa.
Tallennuksessa käytetään transaktiota ja ConcurrencyStamp-tarkistusta.
Viitatun klusterin poisto edellyttää sen reittien ja kohteiden hallittua poistoa;
UI kertoo vaikutuksen ennen käyttäjän vahvistusta.

Validoi koko ehdotettu asetusjoukko ennen tietokantakirjoitusta. Onnistunut
tallennus julkaisee uuden immutable YARP-konfiguraation ja ChangeTokenin heti;
pollaus-/Cron-palvelua ei tarvita. Virheellinen muutos ei saa tyhjentää viimeistä
toimivaa konfiguraatiota. Tietokantakirjoitus ja YARP:n asynkroninen reittijulkaisu
eivät ole yhteinen transaktio: julkaisuvirhe raportoidaan ja viimeinen toimiva
ajokonfiguraatio säilytetään uudelleenlatausta varten. Hallinnassa on Reload-toiminto.

Proxy ei ohita hostin tavallista reittivalintaa tai varaa moduulin
hallintaendpointeille tarkoitettua prefixiä. Hostin ja proxyreitin yhtä tarkka
päällekkäisyys hylätään tai vaatii yksiselitteisen reittiprioriteetin; oletuksena
hostin omat hallinta-/API-reitit säilyvät tavoitettavina myös catch-all-proxyllä.

## Policy ja käyttöliittymä

`proxy.manage` vaatii oletuksena tunnistautumisen ja kanonisen GXRoles.Admin-roolin.
Policy suojaa kaikkia hallintaendpointeja ja yhteistä Config-alueen Proxy-tabia.
Proxyliikenteelle ei lisätä hallintapolicya tai uutta oletusautentikointivaatimusta.
InstallAsync ja StartAsync luovat puuttuvan oletuspolicyn idempotentisti.
Samannimistä olemassa olevaa custom-policya ei ylikirjoiteta tai oteta omistukseen.
UninstallAsync poistaa vain moduulin omistaman muuttamattoman oletuksen;
omistustieto säilyttää alkuperäisen ID:n uudelleenasennusta varten.
Proxydata säilyy poiston jälkeen, mutta proxyreitit ja tabi poistuvat.

Tabi sisältää GXTable-listat klustereille, kohteille ja reiteille sekä suodattimet.
GXMenu-toiminnot ovat Add, Edit, Delete ja Reload. Editorit näyttävät nykyiset
proxykentät. Suodattimen vaihto nollaa sivutuksen. IGXProgress ja siihen liitetty
cancellation tukevat peruutusta. API-virheet esitetään luettavina ProblemDetails-
viesteinä; editori säilyttää syötetyt tiedot validointivirheen jälkeen.

## Julkaisu ja testaus

Addin käyttää nykyistä ZIP-/kehityslataajaa ja jakaa hostin yhteiset Module/Shared-
rajapinnat. YARP ja sen moduulikohtaiset riippuvuudet kuuluvat julkaisutulokseen.
Publish ZIP sisältää assemblyt, riippuvuudet, PDB:t ja sisältötiedostot.
Kehitysasetuksiin lisätään projektirivi muuttamatta tietokanta-asetuksia.
Dokumentoidaan testikonfiguraatio, debugger/restart-kierto, reittien hallinta,
nykyinen liikenteen julkisuus, poisto ja ZIP-asennus.

Testit käyttävät erillistä SQLite-tietokantaa ja paikallista testitaustapalvelinta.
Todellinen AMI-runtime ja hostin routing testaavat HTTP-välityksen, alkuperäisen
metodin/body/queryn, vastauksen status/header/body-kulun, path-transformit,
kuormantasauksen/session affinityn sekä reittien välittömän päivityksen.
Testataan moduulin sammuttaminen käynnissä olevan välityksen aikana, uuden
pyynnön esto, endpoint-lähteen irrotus ja hostin omien reittien tavoitettavuus.

Elinkaari-/policytestit kattavat adminin hallinnan, käyttäjän/anonyymin eston,
julkisen proxyliikenteen, custom-policyn säilymisen, vakaan ID:n ja retained datan.
Skeematesti käyttää vanhaa taulurakennetta ja todentaa datan säilymisen.
Repositorytestit kattavat invalidit asetukset, soft-delete-suodatuksen,
konkurenssivirheen ja viimeisen toimivan konfiguraation säilymisen.
Todellinen kehitys-/ZIP-lataus, host build ja nykyiset moduuli-/policytestit
varmistavat yhteisen toiminnan. Manuaaliset testit raportoidaan erikseen.

Valmis Proxy on itsenäisesti asennettava/poistettava addin; hostiin ei jää
konkreettista Proxy- tai YARP-toimintaa yleistä moduulirouting-tukea lukuun ottamatta.
