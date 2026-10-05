# Gurux.AMI.ContentManager

## Tavoite ja hyväksytty toimintamalli

Siirretään kaikki Content- ja ContentType-toiminnot AMI:n Shared-, server-
ja client-projekteista addiniin. Projektin ja assemblyn nimi on
`Gurux.AMI.ContentManager`, moduulin ID `ContentManager` ja sijainti
`C:/Projects/Gurux.DLMS.AMI4/Gurux.DLMS.AMI.Modules/Gurux.AMI.ContentManager`.
Moduulilla voi määritellä sisältötyyppejä kenttineen ja luoda niihin
perustuvaa sisältöä. `dotnet publish` paketoi julkaistavan build-tuloksen
ZIPiksi. ZIP on moduulin asennuspaketti; sisältödatan vienti on erillinen
olemassa olevan tiedonsiirron toiminto.

Sisältötyypin näkyvyys on oletus, ei yksittäisen sisällön näkyvyyden yläraja.
Käyttäjän vahvistuksen mukaisesti sisältö voi olla tyyppiään julkisempi tai
yksityisempi. Julkinen sisältö näkyy myös anonyymeille. Sisältöä voivat
oletuksena luoda tunnistautuneet käyttäjät. Sisältötyyppejä hallitsee
oletuksena Admin. Tekijä hallitsee omaa sisältöään; Admin hallitsee kaikkea.

## Siirrettävä kokonaisuus

Addin omistaa Content- ja ContentType-DTO:t, kentät ja kenttäarvot, ryhmät,
ryhmäjäsenyydet, sisältökohtaiset käyttäjäasetukset ja yhteistyötaulut,
näkyvyysenumit, REST-mallit, repositoryrajapinnat, repositoryt, HTTP-
endpointit, Razor-sivut, valitsimet, kenttäeditorit, konvertterit,
policyvakiot ja moduulikohtaiset resurssit.

Hostista poistetaan näiden konkreettiset tyypit, DI-rekisteröinnit,
policyjen oletusseedaus, kiinteät valikot/reitit ja käyttäjä-/ryhmä-DTO:iden
Content-navigaatiot. `GXUser`, `GXUserGroup`, policyt ja yleiset
datapalvelut jäävät yhteisiksi. Moduuli tallentaa niiden tunnisteet.
Hostiin ei lisätä ProjectReferencea ContentManageriin.

Nykyiset kytkennät `DataExchangeRepository`, `FavoriteRepository`,
`ComponentViewController`, `GXQuery`, `IGXEventsListener`, workflow-mallit,
käyttäjäryhmähallinta ja sisältöä renderöivät helperit käsitellään siirrossa.
Content-kohtainen logiikka siirtyy addiniin. Tarvittavat hostin integraatiot
käyttävät yleisiä moduulien palvelu-/tapahtuma-/kohderekisteröintejä ja
tunnisteita, eivät addinin DTO:ita. Muut AMI:n kohteet säilyttävät nykyisen
toimintansa.

## Toteutustapa

Käytetään nykyistä runtime-lataajaa, module-owned DI-scopeja,
IAmiModuleServices-dispatcheria ja policyllä suojattua UI-metadataa.
Tämä säilyttää ZIP- ja kehityslatauksen yhteisen polun.

Vaihtoehto, jossa hostin DTO:t ja repositoryt jätetään paikalleen ja
vain UI paketoidaan, ei täytä täydellistä siirtoa. Erillinen
sisältöpalvelin lisäisi tarpeettoman palvelurajan. Valittu ratkaisu on
olemassa olevan sisältötoiminnon siirto itsenäiseksi runtime-addiniksi.

## Policyjen oletukset ja elinkaari

Säilytetään nykyiset vakaat pienaakkosiset policytunnisteet:
`content-type.view`, `content-type.add`, `content-type.edit`,
`content-type.delete`, `content.view`, `content.add`, `content.edit`,
`content.delete`, `content.close` ja vastaavat sisältö-/sisältötyyppiryhmien
policyt. Käyttäjän ContentType.edit tarkoittaa tämän toiminnon edit-policya;
uusia rinnakkaisia kirjoitusasuja ei lisätä.

Oletukset:

- ContentType add/edit/delete ja ryhmien hallinta: authenticated Admin.
- ContentType view: tunnistautuneet luojat voivat lukea luontiin tarvittavan
  tyyppimäärittelyn; muut lukevat vain näkyvän sisällön renderöintiin
  tarvittavan julkisen määrittelyn, eivät hallintatietoja.
- Content view: sallii myös anonyymin julkisen sisällön lukemisen.
- Content add: authenticated-käyttäjät.
- Content edit/delete/close: operaatiopolitiikka sekä tekijyys tai
  hallintaoikeuden antava policy; tavallinen käyttäjä ei saa muokata muiden
  sisältöä pelkän yleisen edit-policyn perusteella.
- Yksityisen sisällön hallintapääsy: erillinen moduulin manage-policy,
  jonka oletusvaatimus on Admin.

`InstallAsync` luo policyt ja niiden GXPolicyRequirement-rivit
idempotentisti. `StartAsync` varmistaa puuttuvat edellytykset myös
kehityslatauksessa, joka ohittaa InstallAsync-hookin. Omistajuus ja luotu
oletus tallennetaan moduulin omaan rekisteröintitauluun.
Olemassa olevat mukautetut policyt säilytetään. Vanhat tunnistettavasti
muuttamattomat hostin oletuspolicyt voidaan siirtää moduulin omistukseen
ja päivittää hyväksyttyihin oletuksiin; mukautettuja rajoituksia ei ohiteta.

`UninstallAsync` poistaa moduulin omat muuttamattomat policyt ja
vaatimukset. Muokattuja tai ennestään muiden omistamia policyjä ei poisteta
perusteetta. Tämä vastaa IP-addinin turvallista omistajuusmallia.
Sisältötauluja ja sisältödataa ei pudoteta uninstallissa.

## Sisältötyypin oletus ja sisällön näkyvyys

Nykyinen `ContentVisibility` kuvaa julkaisuaikaa, sulkemista, etusivua ja
valikkoa. Sen arvoja ei uudelleenkäytetä katseluoikeuksiksi.
Lisätään erillinen pääsynhallintamalli:

- Sisältötyypin katseluoletus: Public, Policy tai CreatorOrPolicy.
- Sisällön katselutila: Inherit, Public, Policy tai CreatorOrPolicy.
- Policy ja CreatorOrPolicy viittaavat vakaaseen GXPolicy-tunnisteeseen.
- Inherit käyttää sisältötyypin nykyistä oletusta. Muut tilat korvaavat sen.
- CreatorOrPolicy sallii tallennetun sisällön tekijän TAI nimetyn policyn
  täyttävän käyttäjän. Tyypin tekijä ei ole sisällön tekijä.
- Policy sallii vain nimetyn policyn täyttävät käyttäjät.
- Uuden tyypin oletus on Public; uuden sisällön tila on Inherit.

Tyypin oletuksen vaihtaminen vaikuttaa Inherit-sisältöihin. Eksplisiittisesti
asetetun sisällön näkyvyys pysyy samana. Luonti-/muokkauslomakkeessa tämä
ero ja julkiseksi vaihtamisen vaikutus kerrotaan käyttäjälle.

Puuttuva, poistettu tai virheellinen policy estää policyyn perustuvan
katselun. CreatorOrPolicy-tilassa tekijän pääsy säilyy, kun policy puuttuu.
Tekijä päätellään palvelimella kirjautuneesta käyttäjästä luonnin aikana;
asiakas ei voi vaihtaa sitä vapaasti myöhemmässä päivityksessä.
Adminin oletusoikeus yksityiseen sisältöön tulee policysta, ei UI:n
piilotetusta tai palvelimen kiinteästä ohituslauseesta.

## Palvelimen oikeustarkistukset

Yhteinen moduulin access evaluator tarkistaa operaatiopolicyn,
voimassaolon/julkaisuajan, poiston tilan ja sisällön efektiivisen
katselusäännön. Samaa logiikkaa käytetään yksittäishauissa, listauksissa,
haku-/lukumäärätuloksissa, sisällön kentissä, liitteissä, etusivussa,
valikoissa, suosikeissa, URL-aliasresoluutiossa, tiedonsiirrossa ja
muutosilmoituksissa. Piilotetun sisällön otsikko, kentät, liitteet tai
olemassaolo eivät vuoda listan metatiedoissa tai ilmoituksissa.

Sisältötyypin hallintarajapinta ei ole yleinen anonyymi rajapinta.
Julkinen sisältö palauttaa renderöintiin tarvittavan tyypin/field-skeeman
erillisen suppean vastauksen kautta. Private-content pääsy ei riipu vain
AuthorizeView-komponentista tai URL:n arvattavuudesta.

## UI ja moduulin julkiset sivut

Addin tarjoaa sisältöjen listauksen, luonti-/muokkausnäkymän,
julkisen katselun ja Adminin sisältötyyppihallinnan. Käytetään GXTablea,
nykyisiä menu-komponentteja, kenttäeditoreita ja policyvalitsinta.
Tarvittavat Content-kohtaiset asetukset ja komponentit siirtyvät addiniin.

Hostin yleinen moduulisivujen metadata/renderöinti laajennetaan tukemaan
myös julkista addinin katselusivua. Nykyinen manifesti-/asset-rajapinta
vaatii kirjautumisen; sen pääsynhallinta laajennetaan nimenomaisella
public-page-policyllä. Anonyymi saa vain julkisen sivun tarvitsemat
moduuliassetit ja metadatan, eikä muiden moduulien hallintapääsy avaudu.
Clientin reitityksen on kyettävä lataamaan sivu suoraan URL:sta, myös
täydellisen sivulatauksen jälkeen.

Moduulin sulkeminen/poisto poistaa sen valikot, sivut, tabit, endpointit ja
service-exportit runtime-rekistereistä. Avatut näkymät reagoivat muutokseen.
Hostin yhteiset tyypit eivät pidä konkreettisia module-Type-viitteitä.

## Skeema ja aiemman sisällön säilyminen

Säilytetään nykyisten taulujen nimet, tunnisteet, viittaukset ja sisältö.
Lisätään näkyvyyden kentät hallitulla migraatiolla. Nykyisiä ryhmä-/rooli-
ja yhteistyörajoituksia ei muuteta julkisiksi automaattisesti: rajoitettu
vanha sisältö mapataan sitä vastaavaan policyyn tai tekijä-/policytilaan.
Aidosti julkinen vanha sisältö säilyy julkisena. Uusien sisältöjen Public-
oletus ei ole lupa avata aiempaa yksityistä dataa.

Sisältötyypin poistaminen ei orpouta olemassa olevia sisältöjä; poistaminen
estetään, kun siihen liittyy poistamattomia sisältöjä. Erillistä tuhoavaa
kaskadipoistoa ei lisätä. Nykyiset ajastus- ja sulkemisominaisuudet säilyvät.

## Publish ja testaus

Publish-ZIP sisältää moduulin assemblyt, yksityiset riippuvuudet,
UI:n tarvitsemat assetit ja sisältötiedostot publish-hakemiston rakenteessa.
AMI:n ja moduulin yhteiset sopimukset jaetaan hostin kanssa nykyisen
lataajamallin mukaisesti. Kehittäminen ei vaadi publishia tai ZIPiä.

Testataan erillisellä SQLite-kannalla todellisen runtimen ja lataajan kautta:
Adminin tyyppihallinta, tavallisen käyttäjän luonti, anonyymin julkinen
katselu, tekijän yksityinen katselu, toisen käyttäjän kielto,
policyn antama pääsy, puuttuva policy, näkyvyyden override molempiin suuntiin,
tekijätunnisteen väärentämisen esto, listojen/lukumäärien/metatietojen
rajaukset, liitteet/alias/tiedonsiirto/ilmoitukset, vanhan yksityisen datan
säilyminen, policyjen install/uninstall, suora julkinen sivulataus,
runtime-poisto ja Publish-ZIP-asennus. Hostin build ja olemassa olevat
policy-, moduuli- ja alias-testit varmistavat siirron regressiot.
