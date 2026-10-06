# Gurux.AMI.EndpointExplorer

## Tavoite

Luodaan addin `Gurux.AMI.EndpointExplorer`, moduulin ID `EndpointExplorer`,
hakemistoon `C:/Projects/Gurux.DLMS.AMI4/Gurux.DLMS.AMI.Modules/Gurux.AMI.EndpointExplorer`.
Moduuli näyttää kaikki Gurux.DLMS.AMI:n reittirekisteriin rekisteröidyt endpointit.
`Gurux.AMI.EndpointExplorer_old` toimii toiminnallisena esimerkkinä; sen erillistä
palvelinta, client-sovellusta tai vanhaa API-reittiä ei kopioida uuteen arkkitehtuuriin.

## Arkkitehtuuri ja tiedon lähde

Moduuli käyttää nykyistä IAmiModule-runtimea, module-owned DI-scopeja ja
policyllä suojattua tabirekisteröintiä. Se ei käynnistä omaa HTTP-palvelinta.
Endpointit luetaan hostin yhteisestä `EndpointDataSource`-rekisteristä käyttäen
moduulin HostServices-palveluntarjoajaa. Toteutuksessa varmistetaan, että lähde
on koko sovelluksen yhdistetty reittirekisteri, ei pelkkä moduulien data source.
Jos nykyinen DI ei tarjoa yhdistettyä lähdettä, hostiin lisätään yleinen
rekisteröinti; host ei viittaa EndpointExplorerin konkreettisiin tyyppeihin.

Rekisteri luetaan jokaisella listahaulla. Listaus sisältää controller-, minimal API-,
UI-, framework- ja aktiivisten addin-moduulien rekisteröidyt endpointit, myös
EndpointExplorerin oman endpointin. Moduulin sulkemisen jälkeen sen poistuneet
endpointit eivät näy seuraavassa haussa. Sama reittimalli voi esiintyä useasti;
eri rekisteröintejä ei yhdistetä tai hukata.

Näytettävät sarakkeet ovat reittimalli, HTTP-metodit ja näyttönimi. Metodimetadatan
puuttuessa näytetään `(any)`. Reittimallin puuttuessa näytetään `(no route pattern)`.
Näyttönimen puuttuessa näytetään tyhjä arvo. Reittimallit näytetään tekstinä,
eivät automaattisesti suoritettavina linkkeinä. Näkymä ei kutsu listattuja endpointteja.
Rekisterin ulkopuoliset middleware-haarat tai fyysiset tiedostot eivät ole
rekisteröityjä endpointteja, joten listaus ei väitä löytävänsä niitä.

## Oikeudet ja elinkaari

Moduuli luo `endpointexplorer.view`-policyn. Oletus vaatii tunnistautumisen ja
kanonisen `GXRoles.Admin`-roolin (`admin`). Sekä Config-tabin metadata että
listan API suojataan tällä policyllä. Tavallinen tunnistautunut käyttäjä ja
anonyymi eivät oletuksena näe tabia eivätkä voi hakea endpointtietoja suoraan APIsta.
Admin voi muuttaa policyn ehtoja olemassa olevalla policyhallinnalla.

InstallAsync luo puuttuvan oletuspolicyn ja omistustiedon. StartAsync tekee saman
idempotentisti kehityslatausta varten, koska se ohittaa InstallAsync:n.
Olemassa olevaa samannimistä policya ei ylikirjoiteta tai oteta automaattisesti
moduulin omistukseen. UninstallAsync poistaa vain moduulin omistaman, sisällöltään
muuttamattoman oletuspolicyn. Muokattu policy säilyy. Omistustieto säilyttää
policytunnisteen uudelleenasennusta varten nykyisten addinien käytännön mukaisesti.

Stop/uninstall poistaa moduulin endpointin ja tabin nykyisen runtimen kautta.
Käynnissä olevat haut käyttävät runtimen cancellation-tokenia. Moduuli ei säilytä
hostin Endpoint-olioita, handler-delegaatteja tai lisäosien tyyppejä omassa välimuistissa.
Vastauksessa palautetaan vain merkkijonometadata, jotta moduulien resursseja ei
pidetä turhaan elossa.

## Käyttöliittymä ja API

Config-alueelle lisätään yhteinen `Endpoints`-tabi.
Se käyttää GXTablea, tekstisuodatinta sekä GXMenu-toimintoa listan päivittämiseen.
Suodatin hakee reitistä, metodeista ja näyttönimestä kirjainkoosta riippumatta.
Tulokset järjestetään reitin ja metodien mukaan sekä tarvittaessa näyttönimellä
vakaan järjestyksen saamiseksi. Suodatus ja kokonaismäärä lasketaan ennen
sivutusta. Listaus tukee GXTablen index/count-parametreja ja rajoittaa yksittäisen
sivun koon 1–100 riviin. Tyhjä lista ja hakuvirhe esitetään selkeästi.
IGXProgress näyttää haun ja mahdollistaa peruutuksen.

API-reitti on `GET /api/extensions/EndpointExplorer/endpoints` ja parametrit
`filter`, `index` ja `count`. Vastaus sisältää `items` ja `total`.
Virheellinen sivutus palauttaa luettavan validointivirheen. Käyttöliittymä näyttää
API:n virheviestin nykyisten addinien ProblemDetails-käytännön mukaisesti.

## Kehitys ja julkaisu

Projekti käyttää nykyisiä yhteisiä Module-, Shared-, ORM- ja UI.Components-
riippuvuuksia. AMI-host ei saa ProjectReferencea addiniin. Moduuliprojekti lisätään
kehitysmoduulien asetuksiin olemassa olevien moduulien rinnalle muuttamatta
tietokanta-asetuksia. Kehitysohje suosittelee erillistä testikonfiguraatiota ja
testitietokantaa.

Publish muodostaa koko julkaisutuloksesta `bin/<Configuration>/EndpointExplorer.zip`-
paketin, joka sisältää moduulin, riippuvuudet, PDB:t ja mahdolliset sisältötiedostot.
Käytetään samaa varsinaista ZIP-/kehityslataajaa kuin muissa addineissa.
Vanhaa esimerkkiprojektia ei poisteta tai muuteta.

## Testaus ja hyväksymiskriteerit

Testataan reittimallit, puuttuva reitti/metodi/näyttönimi, useat HTTP-metodit,
saman reitin erilliset rekisteröinnit, tekstin turvallinen renderöinti, suodatus,
järjestys ja sivutuksen kokonaismäärä. Todellinen hostin reittirekisteri ja
moduuliruntime varmistavat, että hostin ja addinien endpointit näkyvät yhdessä
ja moduulin sulkeminen poistaa sen endpointit seuraavasta hausta.

Todelliset HTTP-/policytestit varmistavat Admin-pääsyn, tavallisen käyttäjän ja
anonyymin eston sekä policyn muokkauksen vaikutuksen APIin ja UI-metadataan.
Elinkaaritestit kattavat idempotentin alustuksen, muokatun policyn säilymisen,
muuttamattoman oletuksen poistamisen ja vakaan tunnisteen uudelleenasennuksessa.
Testit käyttävät erillistä SQLite-testitietokantaa, eivät käyttäjän tuotantodataa.
Publishin tuottama ZIP ladataan todellisella moduulilataajalla. Host build ja
nykyiset moduuli-/policytestit varmistavat yhteisen toiminnan säilymisen.

Valmis moduuli näyttää AMI:n rekisteröidyt endpointit policyllä suojatussa
Config-tabissa, reagoi ajonaikaisiin reittimuutoksiin seuraavassa haussa ja on
itsenäisesti asennettava sekä poistettava ZIP-addin.
