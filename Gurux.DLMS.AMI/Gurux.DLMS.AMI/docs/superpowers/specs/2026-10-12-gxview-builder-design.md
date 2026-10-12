# GXView näkymäeditorin suunnitelma

GXView laajennetaan tallennettavaksi taulukkonäkymäksi, jonka käyttäjä voi rakentaa valitsemalla tietokantataulun, näytettävät sarakkeet, niiden otsikot, liitokset ja muotoilut. Näkymässä voi näyttää kaikki tulosrivit tai käyttää sivutusta sekä valita GXBlockin sivun headeriksi ja footeriksi. Schedules ja ScheduleLogs käyttävät samaa näkymämallia ja säilyttävät suodatus-, lajittelu- ja rivitoimintonsa.

Käyttäjä on hyväksynyt tämän arkkitehtuurin keskustelussa. Tämä dokumentti määrittää toteutettavan käyttäytymisen ja rajapinnat tarkistettavaksi ennen toteutussuunnitelmaa.

## Nykyinen rakenne ja muutoksen rajat

AMI:n nykyinen GXView sisältää käyttäjän tunnisteen, näkymän nimen ja GXViewColumn-listan. Sarakkeessa on tunniste, nimi, näkyvyys ja sijainti. ViewController käyttää tietokantaa suoraan, ja GXViewDefinitions sallii vain schedules- ja schedule-logs-sarakkeet. Nämä vastuut siirretään repositoryyn ja metatietopalveluun.

IGXViewRepository seuraa IMenuRepositoryn CRUD-, luku-, listaus-, peruutus- ja käyttäjäselvitysrakennetta. GXViewRepository seuraa MenuRepositoryn palveluriippuvuuksia, käyttöoikeustarkistuksia ja ilmoituskäytäntöjä. ViewController on repositorya kutsuva ohut controller, kuten MenuController. Menun Regenerate-toimintoa ei kopioida: näkymää varten tarjotaan määritelmän validointi ja esikatselu.

Gurux.UI.Components on erillinen paikallinen projekti hakemistossa C:/Projects/Gurux.UI.Components. Sen editori ja renderer käyttävät kirjaston omia näkymämalleja ja palvelurajapintoja. Ne eivät viittaa AMI:n DTOihin, ORM:ään tai ResourceStorageen. AMI muuntaa tallennetut DTOt kirjaston malleiksi ja toteuttaa palveluadapterit.

Rajapintojen taaksepäin yhteensopivuutta ei vaadita. Nykyinen AMI-asiakas ja testit päivitetään uuden API:n mukaisiksi. Tietokannan skeemamuutokset tehdään projektin nykyisellä päivitysmekanismilla olemassa olevia käyttäjätietoja poistamatta.

## Tietomalli

### GXView

Näkymä tallentaa seuraavat tiedot:

- Id, UserId ja käyttäjäkohtaisesti yksilöllinen Name.
- Title ja Description käyttäjälle näytettäväksi.
- SourceTable: metatietoluettelon vakaa taulutunniste, ei SQL-lause.
- RowMode: AllRows tai GroupBySource. AllRows säilyttää liitosten tulosrivit; GroupBySource palauttaa yhden rivin päätaulun tietuetta kohti.
- ShowAllData ja PageSize. Uuden näkymän oletus on sivutus ja 20 riviä sivulla.
- HeaderBlockId ja FooterBlockId, kumpikin valinnainen GXBlock-viittaus.
- CreationTime, Updated, Removed ja ConcurrencyStamp nykyisten hallittavien resurssien käytäntöjen mukaisesti.
- Columns ja Joins erillisinä lapsitauluina, jotka ladataan repositoryssa.

Näkymät ovat käyttäjäkohtaisia. Omistaja hallitsee omia näkymiään tarvittavilla GXView-oikeuksilla; ylläpitäjä voi hallita kaikkien käyttäjien näkymiä. Tämä muutos ei lisää näkymien jakamisen käyttöliittymää. GetUsersAsync palauttaa käyttäjät, joilla on oikeus kyseiseen näkymään, ilmoituksia varten.

### GXViewColumn

Sarakkeen määritelmä tallentaa:

- Id ja ViewId.
- Name: vakaa, näkymän sisällä yksilöllinen saraketunniste. Sitä käyttävät suodatin, otsikko, rivisolu, lajittelu ja tulosprojektion avain.
- SourceAlias ja SourceColumn: päätaulun tai liitetyn taulun alias sekä fyysisen kentän metatietotunniste.
- Text: käyttäjän antama otsikkoteksti. Tyhjä teksti käyttää lähdekentän oletusotsikkoa.
- Position ja Visible. Sarakkeita voi lisätä, poistaa, järjestää ja piilottaa.
- Aggregate: None, Count, CountDistinct, Sum, Average, Min tai Max.
- Formatter: rekisteröidyn formatterin tunniste sekä sen asetukset. Asetuksiin kuuluvat tarvittaessa muotoilumerkkijono, enum-resurssityyppi, avain–teksti-mäppäys ja tyhjän arvon teksti.

Saman lähdekentän voi näyttää useassa sarakkeessa eri otsikolla tai muotoilulla. Näillä sarakkeilla on eri Name-tunnisteet. Fyysistä lähdekenttää ei käytetä tulosrivin yksilöllisenä avaimena.

### GXViewJoin

Liitos tallentaa Id:n, ViewId:n, järjestyksen, lähdealiaksen ja -kentän, kohdetable-tunnisteen, kohdealiaksen ja -kentän sekä tyypin Left tai Inner. Metatietopalvelu kertoo liitoksen kardinaliteetin ja tarjoaa tunnetut viiteavaimet oletusvalintoina. Käyttäjä voi valita yhteensopivien avainkenttien välisen liitoksen myös silloin, kun tietokannassa ei ole fyysistä viiteavainta. Palvelin validoi sen.

Aliakset erottavat saman taulun eri liitokset, kuten luojan ja viimeksi muokanneen käyttäjän. Liitosketju voi käyttää aiemmin lisättyä aliasia. Irralliset liitokset, duplikaattialiakset ja kiertävät riippuvuudet hylätään.

## Repository ja API

IGXViewRepository sijoitetaan Shared/DIs-hakemistoon. Se sisältää seuraavat metodit IMenuRepositoryn parametrikäytäntöjen mukaisesti:

- ListAsync(ListViews?, ListViewsResponse?, CancellationToken).
- ReadAsync(Guid, Expression<Func<GXView, object>>?, CancellationToken).
- ReadAsync(string, Expression<Func<GXView, object>>?, CancellationToken).
- UpdateAsync(IEnumerable<GXView>, Expression<Func<GXView, object>>?, CancellationToken), palauttaa tunnisteet.
- DeleteAsync(IEnumerable<Guid>, bool delete, bool notify, CancellationToken).
- GetUsersAsync(Guid?, CancellationToken) ja GetUsersAsync(IEnumerable<Guid>?, CancellationToken).
- GetTablesAsync, GetTableAsync, ValidateAsync ja QueryAsync metatietoa, validointia ja tiedonhakua varten.

GXViewRepository lisätään ServerSettings.AddRepositories-rekisteröintiin. Määritelmän, sarakkeiden ja liitosten päivitys on yksi transaktio. Omistaja määräytyy palvelimella; asiakas ei voi vaihtaa sitä. ConcurrencyStamp tarkistetaan ennen päivitystä, ja ristiriita palautetaan asiakkaalle ilman viimeisimmän tallennuksen ylikirjoitusta.

Shared/Rest sisältää GetViewResponse-, ListViews-, ListViewsResponse-, UpdateView-, UpdateViewResponse-, RemoveView- ja RemoveViewResponse-sopimukset sekä metatieto-, validointi- ja query-sopimukset. Listaus tukee hakuehtoja, lajittelua, sivutusta, mukana olevia ja pois rajattuja tunnisteita sekä ylläpitäjän AllUsers-valintaa kuten menulistaus.

ViewControllerin reitit ovat:

| Reitti | Tarkoitus |
| --- | --- |
| GET api/View?id=… | Luku tunnisteella |
| GET api/View/name?name=… | Luku nimellä |
| POST api/View/List | Näkymien listaus |
| POST api/View/Add ja api/View/Update | Määritelmän tallennus |
| POST api/View/Delete | Poisto tai Removed-merkintä |
| GET api/View/Tables | Käytettävissä olevat taulut |
| GET api/View/Table?name=… | Sarakkeet, avaimet ja tunnetut liitokset |
| POST api/View/Validate | Tallentamattoman määritelmän tarkistus |
| POST api/View/Preview | Tallentamattoman määritelmän rajattu esikatselu |
| POST api/View/Query | Tallennetun näkymän tulosrivit |

Esikatselu näyttää enintään 20 riviä ja ilmoittaa kokonaismäärän. Se ei tallenna määritelmää. Query saa näkymän tunnisteen, sivutusparametrit, saraketunnisteeseen perustuvan lajittelun ja tyypitetyt suodatusehdot. Asiakas ei lähetä SQL:ää.

GXView-oikeuksien View, Add, Edit ja Delete lisäksi metatietojen ja tietokyselyjen suoritus tarkistaa lähderesurssien oikeudet. Näkymän hallintaoikeus ei anna pääsyä sen lähdetaulun kaikkiin riveihin.

## Metatiedot ja tietokysely

Palvelin muodostaa valittavien taulujen luettelon ORM:n ja asennettujen moduulien rekisteröidyistä entiteeteistä sekä olemassa olevasta skeemasta. GXViewDefinitionsin nykyinen kahden taulun sallintalista korvataan laajennettavalla katalogilla. Katalogi sisältää taulujen vakaat tunnisteet, näyttönimet, tyypitetyt kentät, avaimet ja suhteet sekä resurssin käyttöoikeusadapterin.

Adapteri soveltaa samoja käyttäjä-, ryhmä-, resurssipolicy- ja poistettujen rivien rajoituksia kuin resurssin nykyinen repository. Samat rajoitukset koskevat liitettyjen taulujen rivejä ennen aggregointia. Taululle, jonka rivioikeuksia ei voida ratkaista, ei anneta tavalliselle käyttäjälle kyselyoikeutta. Salasanat, tokenit, salausavaimet ja vastaavat sisäiset kentät eivät kuulu editorin kenttäluetteloon tai tulosprojektioon.

Kyselysuunnittelija erotetaan GXViewRepositoryn CRUD-koodista. Se validoi aliakset, lähteet, tyypit, liitokset, aggregoinnit ja lajittelun ja muodostaa parametrisoidun kyselyn nykyisen ORM:n ja tietokantaproviderin kautta. Tunnisteet ratkaistaan metatiedoista. Suodattimet käyttävät kentän tietotyyppiä ja sille sallittuja operaatioita: vertailut, tekstihaku ja null-tarkistus. Formatterin näyttötekstiä ei käytetä tietokannan lajitteluarvona.

Tulos sisältää sarakkeiden metatiedot, rivit, kokonaismäärän ja päätaulun tietueen tunnisteen. Kukin rivi sisältää myös oman vakaan tulosavaimensa: AllRows-tilassa saman päätaulutunnisteen eri liitosrivit eivät saa samaa käyttöliittymäavainta. Aggregoidussa tilassa päätaulun tunniste yksilöi rivin.

## Yksi–moneen-liitosten semantiikka

AllRows näyttää kaikki käyttöoikeuksien sallimat liitosrivit. Jos yhdellä mittarilla on kolme tapahtumaa, mittari näkyy kolmella rivillä. Jos näkymässä on kaksi itsenäistä yksi–moneen-liitosta, niiden yhdistelmä voi tuottaa rivien ristitulon; editori näyttää tämän vaikutuksen liitosten yhteenvedossa ja esikatselussa.

AllRows-tilassa sarakkeiden Aggregate on None. Aggregaatin valinta edellyttää GroupBySource-tilaa; editori kertoo vaatimuksesta ja palvelin hylkää ristiriitaisen määritelmän.

GroupBySource näyttää yhden rivin päätaulun tietuetta kohti. Päätaulun ja yksi–yhteen/moneen–yhteen-liitosten kentät voidaan näyttää sellaisinaan. Yksi–moneen-polun kentälle on valittava aggregointi; määritelmä hylätään, jos valinta puuttuu.

Kukin itsenäinen yksi–moneen-haara aggregoidaan ennen sen yhdistämistä päätauluun. Esimerkiksi mittarin kaksi virhettä ja kolme tapahtumaa tuottavat lukumäärät 2 ja 3, eivät kumpikin 6. Ketjutetussa haarassa laskenta kohdistuu käyttäjän valitseman aliaksen kenttään. Count laskee ei-null-arvot; lapsen pääavaimen Count on lapsirivien määrä. Left-liitos ilman lapsirivejä tuottaa lukumäärän 0, muiden aggregaattien arvoksi null.

Sum ja Average sallitaan numeerisille kentille. Min ja Max sallitaan vertailukelpoisille kentille. CountDistinct poistaa saman valitun kentän arvon toistumat kyseisen päätaulun tietueen sisällä. Näkymän aggregointitila määräytyy RowModesta, eikä piilotetun sarakkeen togglaus muuta sitä.

## Sivutus ja lajittelu

ShowAllData näyttää kaikki valmiin kyselytuloksen rivit ja piilottaa pagerin. Sivutetussa tilassa PageSize määrittää sivun koon. Suodatus, liitokset, oikeusrajaukset ja aggregointi tehdään ennen sivutusta. Kokonaismäärä tarkoittaa AllRows-tilassa tulosrivejä ja GroupBySource-tilassa ryhmiteltyjä päätaulun tietueita.

Lajittelu voi käyttää näkyvän lähdesarakkeen tai aggregaatin tyypitettyä arvoa. Tasatilanteet ratkaistaan päätaulun ja tarvittavien liitostaulujen avaimilla, jotta sivurajat ovat vakaat. Lajittelun tai suodatuksen muutos palauttaa ensimmäiselle sivulle. GXTable säilyttää nykyiset nouseva-, laskeva- ja lajittelu pois -tilat sekä FilterContent-, HeaderContent- ja ItemContent-laajennuspisteet.

## Gurux.UI.Components editori ja renderer

Kirjastoon lisätään GXViewEditor sekä dynaamisen näkymän näyttävä GXViewTable. Ne käyttävät neutraaleja näkymä-, sarake-, liitos- ja metatietomalleja. Tallennus, metatiedot, kysely ja muotoilu toimitetaan adaptereina tai callbackeina.

Editorin käyttöpolku on:

1. Valitse päätaulu. Taulun vaihtaminen ilmoittaa jo määriteltyjen sarakkeiden ja liitosten poistumisesta ennen vaihtoa.
2. Lisää päätaulun tai liitetyn taulun kenttiä näytettäviin sarakkeisiin. Muokkaa otsikkoa, sijaintia ja näkyvyyttä tai poista sarake.
3. Lisää tarvittavat liitokset ja valitse Left/Inner. Tunnetut viiteavaimet tarjotaan oletuksina.
4. Valitse kaikki liitosrivit tai yksi rivi päätaulun tietuetta kohti. Valitse tarvittavat aggregaatit sarakkeille.
5. Valitse sarakekohtainen formatteri ja sen asetukset.
6. Valitse kaikki tiedot tai pager sekä sivukoko. Valitse valinnainen header- ja footer-block AMI:n toimittamasta block-luettelosta.
7. Tarkista esikatselu ja tallenna. Validointivirhe osoittaa virheelliseen sarakkeeseen tai liitokseen, ja editori säilyttää syötetyt tiedot.

GXViewTable muodostaa suodattimet tietotyyppien perusteella, otsikot Text-kentästä ja rivisolut projektioavaimista. Sovellus voi korvata sarakkeen sisältöfragmentin esimerkiksi navigointilinkillä, tilapisteellä tai omalla selectorilla. Sarakkeiden järjestys ja näkyvyys ovat samat kaikissa kolmessa fragmentissa.

AMI:in lisätään näkymien listaus-, muokkaus- ja näyttösivut sekä navigointiin käytettävät reitit. Tallennetun näkymän voi avata tunnisteella, ja sen voi valita GXComponentView/GXBlock-käyttöön projektin nykyisen komponenttirakenteen kautta.

## Formatterit ja GXBlockit

Formatteri saa raaka-arvon, lähdekentän tyypin, sarakkeen asetukset ja nykyisen kulttuurin. Kirjaston oletusformatterit käsittelevät tekstin, numerot, booleanin, päivämäärän ja ajan, Guidin ja null-arvon. Mäppäysformatterin avaimet verrataan tyypitettyyn raaka-arvoon; tuntematon arvo näyttää oletusmuotoilun.

AMI:n enum-formatter käyttää ResourceStorage.GetEnumTypea asetusten enum-resurssityypillä. Tarvittavat enum-resurssit ladataan ennen rivien muotoilua, ja tuntematon enum-arvo näyttää raaka-arvon. AMI:n aikamuotoilu käyttää ResourceStoragen nykyisiä kulttuuri- ja aikavyöhykekäytäntöjä. Näytettävät arvot HTML-koodataan tavallisena tekstinä.

Creator-nimen tai sähköpostin näyttäminen tehdään liitoksella GXUser-tauluun, ei formatterin riviä kohti tekemällä HTTP-haulla. Käyttäjä voi näyttää molemmat eri sarakkeissa.

HeaderBlockId ja FooterBlockId ladataan AMI:n IBlockRepositoryn kautta oikeuksien tarkistamisen jälkeen. AMI sijoittaa niiden nykyisen block-renderöinnin näkymätaulukon ylä- ja alapuolelle ja toimittaa sen kirjastolle fragmentteina. Tyhjä block-valinta ei renderöi blockia. Puuttuva tai poistettu block ilmoitetaan; itse taulukko pysyy käytettävissä.

## Schedules ja ScheduleLogs

Näille näkymille määritetään oletus-GXView-mallit nykyisten sarakkeiden, otsikoiden, suodattimien ja lajittelun perusteella. Käyttäjä voi muokata niitä editorilla ja lisätä esimerkiksi luojan nimen tai sähköpostin. ScheduleLogs säilyttää resurssityypin ScheduleLog rajauksen, jotta muiden lokityyppien tietoja ei tule samaan näkymään.

GXViewSettings päivitetään käyttämään repository-pohjaista ViewControlleria. Tallennus ei enää vaadi kaikkien oletussarakkeiden sisällyttämistä määritelmään, eikä GXViewDefinitions.Normalize määrää kiinteää järjestystä. Nykyiset schedule- ja lokirivin linkit, valinta ja toiminnat kytketään päätaulun tunnisteeseen. Yhden päätaulun tietueen useat tulosrivit eivät monista Run-, Close- tai Delete-toiminnon kohteita.

## Virheet, skeema ja testaus

Puuttuva oikeus palauttaa käyttöoikeusvirheen. Tuntematon tai poistunut taulu, sarake, liitos tai formatteri palauttaa ymmärrettävän määritelmävirheen; kyselyä ei suoriteta osittaisena. Sivutusparametrit validoidaan. Peruutustoken välitetään tietokantaan ja resurssihakuun. Tallennusvirhe säilyttää aiemman tietokantamääritelmän ja editorin tallentamattoman tilan.

Skeemapäivitys lisää GXViewn ja GXViewColumnin uudet kentät sekä GXViewJoin-taulun. Päivitys kattaa uuden ja olemassa olevan tietokannan. Aiemmin tallennettujen schedules- ja schedule-logs-valintojen lähteet ja otsikot täydennetään niiden tunnettujen oletusmallien avulla; näkymän omistaja ja piilotukset säilyvät. Päivitys ei muuta muiden resurssien sisältöä.

Hyväksymistestit kattavat:

- Repositoryn CRUD, luku nimellä ja tunnisteella, listaus, soft/hard delete, käyttäjärajaus, ilmoitukset, concurrency ja transaktion rollback.
- Katalogin taulut, kenttien tyypit, aliakset, tunnetut liitokset ja käyttöoikeusrajaukset.
- Creator.UserName- ja Creator.Email-projektiot sekä saman taulun useat liitokset.
- Left-liitos ilman osumaa ja Inner-liitos, yksi–moneen-rivit, ryhmittely, Count/CountDistinct ja numeeriset aggregaatit.
- Kahden itsenäisen yksi–moneen-haaran lukumäärät ilman ristitulon monistamaa laskentaa.
- Sivutus ja kokonaismäärä molemmissa RowMode-tiloissa sekä ShowAllData, suodatus ja vakaa lajittelu.
- Editorin sarakkeiden lisäys, poisto, otsikko, järjestys, aggregointi, formatteri, block-valinnat, esikatselu ja tallennusvirhe.
- Rendererin yhtenevä sarakejärjestys, tyhjä valinta, enum- ja mäppäysformatterit sekä null-arvot.
- Schedules- ja ScheduleLogs-toimintojen toiminta muokatuilla sarakkeilla ja usealla tulosrivillä.
- Tietokannan uuden ja olemassa olevan skeeman päivitys.

Nykyiset TableColumns- ja ViewSettings-testiharnessit päivitetään ja laajennetaan. Rakennustarkistus tehdään sekä Gurux.UI.Componentsille että AMI-palvelimelle ja -asiakkaalle. Tietokantakyselyjen SQL-generointi tarkistetaan sovelluksen tuetuille providereille; integraatiotestit käyttävät erillisiä testitietokantoja.
