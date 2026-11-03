# MeWorld (RoWorld / OpenFront Clone) - Master Pipeline & Görev Ağacı

> **YENİ GELEN AJAN İÇİN TALİMAT (KULLANICI "DEVAM ET" DEDİĞİNDE):**
> 1. Bu dosyadaki (`PIPELINE.md`) görevleri yukarıdan aşağıya doğru sırayla takip et.
> 2. Durumu `[~]` (Yarım Kalmış) veya `[ ]` (Başlanmamış) olan **İLK** görevi bul.
> 3. Görevin altında yazan **Dosya Yolu**, **İçerik Detayı** ve **Kabul Kriterleri**'ni eksiksiz uygula.
> 4. Kodu yazdıktan sonra terminalde `unity recompile --project-path "c:\Users\mehme\OneDrive\Masaüstü\MeWorld" --format json` çalıştır.
> 5. Hata yoksa görevin başındaki `[ ]` işaretini `[x]` yap ve vakit kaybetmeden bir sonraki `[ ]` görevine geç!

---

## Durum Göstergeleri
- `[x]` : Tamamlandı, derlendi ve doğrulandı.
- `[~]` : Üzerinde çalışılıyor / yarım kaldı (Önce bunu bitir!).
- `[ ]` : Henüz başlanmadı.

---

## FAZ 0: Kurallar, Mimari Anayasa ve Görev Ağacı Kurulumu
- [x] **Görev 0.1:** Halüsinasyon önleyici `AGENTS.md` ve `GEMINI.md` kurallarının oluşturulması.
- [x] **Görev 0.2:** En düşük zekalı yapay zekanın bile adım adım uygulayabileceği `PIPELINE.md` görev ağacının oluşturulması.

---

## FAZ 1: Çekirdek Veri Yapıları, Konfigürasyon ve Harita (Grid) Motoru
- [x] **Görev 1.1: Çekirdek Enum'lar ve Oyun Konfigürasyonu (`Assets/Scripts/Core/GameConfig.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Core/GameConfig.cs`
  - **Yapılacaklar:**
    - `MeWorld.Core` namespace'i altında aşağıdaki tipleri tanımla:
    - `TerrainType` enum: `DeepOcean = 0`, `ShallowWater = 1`, `Plains = 2`, `Highlands = 3`, `Mountains = 4`, `Scorched = 5` (Nükleer bomba sonrası krater/radyasyonlu alan).
    - `StructureType` enum: `None = 0`, `City = 1`, `Port = 2`, `Factory = 3`, `DefensePost = 4`, `SAMLauncher = 5`, `MissileSilo = 6`, `AtomBomb = 7`, `HydrogenBomb = 8`, `Warship = 9`, `Alliance = 10`.
    - `GameConfig` static/sabit değerler sınıfı:
      - `MapWidth = 560`, `MapHeight = 320`
      - `TickInterval = 0.1f` (Saniyede 10 tick)
      - `WinControlPercentage = 0.80f` (`%80` dünya hakimiyeti hedefi)
      - `InitialProtectionSeconds = 157f` (Görseldeki `2:37` koruma süresi)
      - `PlayerNationCount = 88` (88 ulus: 1 gerçek oyuncu + 87 AI ulusu)
      - `TribeCount = 110` (Yüzlerce kabile / küçük bağımsız bölge)
      - Bina maliyetleri (`GetActionCost(StructureType)`):
        - `City` = `250000`, `Port` = `125000`, `Factory` = `125000`, `DefensePost` = `50000`, `SAMLauncher` = `1500000`, `MissileSilo` = `1000000`, `AtomBomb` = `750000`, `HydrogenBomb` = `5000000`, `Warship` = `250000`, `Alliance` = `0`.
      - Sayı formatlayıcı yardımcı metot `FormatNumber(double val)`: `6430 -> "6.43K"`, `775000 -> "775K"`, `1500000 -> "1.5M"`, `<1000` ise tam sayı döndürür.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 1.2: Dünya Haritası Hücre ve Prosedürel Kıta Üreticisi (`Assets/Scripts/Map/WorldMapGrid.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Map/WorldMapGrid.cs`
  - **Yapılacaklar:**
    - `MeWorld.Map` namespace'i altında `WorldMapGrid` sınıfını oluştur.
    - Tek boyutlu (1D flat array, `size = Width * Height`) yüksek performanslı diziler tut:
      - `TerrainType[] Terrain`
      - `float[] Elevation` (`0f..1f` yükseklik)
      - `ushort[] OwnerId` (`0` = Sahipsiz/Deniz, `1..N` = Ulus/Kabile ID)
      - `bool[] IsBorder` (Komşu 4 hücreden biri farklı `OwnerId` ise `true`)
      - `bool[] IsCoast` (Kara hücresi olup en az 1 komşusu deniz ise `true`)
    - **Prosedürel Dünya/Kıta Üretimi (`GenerateProceduralWorld(int seed)`):**
      - RoWorld görselindeki gibi gerçekçi yarımadalar (Hindistan yarımadası, Umman/Arap yarımadası, boğazlar, adalar ve iç denizler) oluşturan çok katmanlı `Mathf.PerlinNoise` (Octave Fractional Brownian Motion - fBm) + bölgesel kıta çekirdekleri (Gaussian falloff blobs) kullan.
      - Karaların toplam haritaya oranı yaklaşık `%45 - %55` olsun.
      - Deniz derinliğini kıyıya yakın yerlerde `ShallowWater` (açık mavi), derinlerde `DeepOcean` (orta mavi) olarak belirle.
      - Karaları yüksekliğe göre `Plains` (yeşilimsi krem/bej), `Highlands` (açık kahve/gri), `Mountains` (yüksek zirveler) olarak ayır.
      - Yardımcı metotlar: `InBounds(int x, int y)`, `GetIndex(int x, int y)`, `IsLand(int index)`, `GetNeighbors4(int x, int y, Span<int> / int[] buffer)`.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 1.3: Piksel Tabanlı Harita Görselleştirici ve Kamera Kontrolcüsü (`Assets/Scripts/Map/MapRendererAndCamera.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Map/MapRendererAndCamera.cs`
  - **Yapılacaklar:**
    - `MapRenderer` (`MonoBehaviour`):
      - `Texture2D` (`Width x Height`, `TextureFormat.RGBA32`, `FilterMode.Bilinear` veya `Point`) ve `Color32[] pixelBuffer` oluşturur.
      - Bir `SpriteRenderer` üzerinden sahnede (`Vector3.zero` merkezli, `PixelsPerUnit = 10f` olacak şekilde `56x32` dünya birimi boyutunda) haritayı gösterir.
      - `Dirty` bayrağı (`isDirty`) olduğunda her karede en fazla 1 kez `RefreshTexture()` çalıştırarak:
        - Deniz piksellerini RoWorld renklerinde (`DeepOcean = #5E97D1`, `ShallowWater = #7BB0E3`, kıyı köpüğü çizgisi `#9BC7F0`) boyar.
        - Sahipsiz kara piksellerini RoWorld'ün karakteristik açık krem/yeşilimsi topoğrafik tonlarında (`Plains = #DCE5C9`, `Highlands = #CBD4B8`, `Mountains = #B5BFA1`, `Scorched = #3B3532`) boyar.
        - Sahipli (`OwnerId > 0`) kara piksellerinde:
          - Eğer `IsBorder == true` ise: Ulusun koyu/canlı sınır rengini (`BorderColor`) basar (Oyuncu için parlak pembe `#E93B81`, diğer uluslar için mavi `#2B6CB0`, turuncu `#DD6B20`, yeşil, mor vb.).
          - Eğer iç bölge ise (`IsBorder == false`): Arazi rengi ile ulusun iç rengini (`FillColor`) `%60` oranında harmanlar (`Color32.Lerp`).
    - `CameraController2D` (`MonoBehaviour`):
      - `UnityEngine.InputSystem` (`Mouse.current`, `Keyboard.current`) kullanarak:
        - Fare tekerleği (`scroll.ReadValue().y`) ile akıcı Zoom (`orthographicSize` `3f` ile `18f` arası, imleç merkezli zoom!).
        - Orta fare tuşu (`middleButton`), Sağ fare tuşu sürükleme veya `WASD` / Yön tuşları ile haritada gezinme (Pan) ve harita sınırları içinde (`Clamp`) tutma.
        - Dünya koordinatını harita hücre koordinatına (`WorldToGrid(Vector3 worldPos, out int gx, out int gy)`) ve tersine (`GridToWorld(int gx, int gy)`) çeviren metotlar.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

---

## FAZ 2: Uluslar, Kabileler, Ekonomi, Fetih (Saldırı Dalgaları) ve Binalar
- [x] **Görev 2.1: Ulus/Kabile Veri Modeli ve İsim Havuzu (`Assets/Scripts/Nations/NationModels.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/NationModels.cs`
  - **Yapılacaklar:**
    - `MeWorld.Nations` namespace'i altında:
    - `NationData` sınıfı:
      - `ushort Id` (`1` = Oyuncu "SnickTrix", `2..88` = AI Uluslar, `89..198` = Kabileler/Tribes).
      - `string Name` (Görseldeki gerçek isimleri içeren zengin liste: `"SnickTrix"`, `"India"`, `"Oman"`, `"Fernhold"`, `"Tideborn"`, `"Mooncrest"`, `"Flintcarvers"`, `"Frostwardens"`, `"Yak Tribe"`, `"Mammoth Moot"`, `"Bramblefire"`, `"Schreckseers"`, `"Pelican Folk"`, `"The Pinebridlers"`, `"Unrivaled Company"` + 180 özgün strateji/kabile ismi).
      - `bool IsPlayer`, `bool IsTribe`, `bool IsAlive`.
      - `Color32 FillColor`, `Color32 BorderColor`.
      - `double Troops`, `double MaxTroops`, `double TroopGrowthPerSec`.
      - `double Gold`, `double GoldGrowthPerSec`.
      - `int TileCount` (Sahip olunan piksel sayısı), `float WorldControlPercent`.
      - `Vector2 CenterGridPos` (Etiket ve başkent ikonu için ağırlık merkezi), `int CapitalX`, `int CapitalY`.
      - `HashSet<ushort> Allies` (İttifak yapılan uluslar).
      - `int CityCount`, `int PortCount`, `int FactoryCount`, `int DefensePostCount`, `int SiloCount`, `int SamCount`.
    - `AttackWave` sınıfı (Aktif saldırı dalgası):
      - `ushort AttackerId`, `ushort TargetOwnerId` (`0` ise sahipsiz toprağa yayılma).
      - `double RemainingTroops` (Saldırıya ayrılan kalan asker).
      - `List<int> FrontierIndices` (Saldırının o an temas ettiği sınır pikselleri).
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 2.2: Toprak Dağıtımı, Fetih Motoru ve Ekonomi (`Assets/Scripts/Nations/TerritoryAndCombatManager.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/TerritoryAndCombatManager.cs`
  - **Yapılacaklar:**
    - `TerritoryAndCombatManager` (`MonoBehaviour`):
      - **Başlangıç Dağıtımı (`SpawnNationsAndTribes()`):**
        - Haritadaki kara pikselleri üzerinde birbirine minimum mesafede 1 Oyuncu (`SnickTrix`, pembe renkli, yarımadanın ortasında), 87 AI Ulus (`India`, `Oman`, `Fernhold`, `Tideborn` vb.) ve ~110 küçük Kabile (`Tribe`) başlangıç noktası seçer ve her birine başlangıçta `35 - 90` piksellik dairesel bir ülke toprağı verir.
        - Oyuncuya başlangıçta `7470` asker (`7.47K`) ve `775000` altın (`775K`) verir (görselle birebir uyumlu başlangıç!).
      - **Sınır ve Merkez Hesaplama (`RecalculateBordersAndCenters()`):**
        - Değişen piksellerin `IsBorder` durumunu, her ulusun `TileCount`, `WorldControlPercent` ve `CenterGridPos` (ülke yazısının duracağı en geniş iç merkez) koordinatlarını günceller.
      - **Ekonomi Döngüsü (`TickEconomy(float dt)`):**
        - Her ulus için `MaxTroops = 5000 + (TileCount * 35) + (CityCount * 25000)`.
        - Lojistik asker büyümesi: `ratio = Troops / MaxTroops`, `TroopGrowthPerSec = (100 + TileCount * 1.2 + CityCount * 180) * Mathf.Clamp01(1.15f - (float)(ratio * ratio))`.
        - Altın büyümesi: `GoldGrowthPerSec = 50 + (TileCount * 2.5) + (CityCount * 350) + (PortCount * 600) + (FactoryCount * 950)`.
      - **Saldırı ve Fetih Çözümleyici (`StartAttack(ushort attackerId, ushort targetOwnerId, float ratio)` ve `TickCombat()`):**
        - Saldıran ulusun mevcut askerinin `ratio` (`0.10f` .. `1.00f`) kadarını düşer ve `AttackWave` başlatır (eğer aynı hedefe aktif dalga varsa askerleri ona ekler).
        - Her tick'te `AttackWave`, saldıran ulusun hedef ulusa (`targetOwnerId`) komşu olan sınır piksellerini bulur ve piksel başına savunma maliyetine göre (`TargetOwnerId == 0` ise piksel başı `8` asker; dolu ulus ise hedef ulusun piksel başı asker yoğunluğu + `DefensePost` varsa `2.5x` çarpan) pikselleri fetheder (`OwnerId = AttackerId`).
        - Deniz aşırı çıkarma (`Naval Invasion`): Eğer saldıranın `Port`'u varsa ve kıyıdaki düşman pikseline tıkladıysa bir çıkarma gemisi/noktası oluşturup oradan fetih başlatır.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 2.3: Binalar, Nükleer Silahlar ve Savaş Gemileri Sistemi (`Assets/Scripts/Structures/StructureAndNukeManager.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Structures/StructureAndNukeManager.cs`
  - **Yapılacaklar:**
    - `MeWorld.Structures` namespace'i altında:
    - `StructureInstance` sınıfı: `int Id`, `StructureType Type`, `ushort OwnerId`, `int GridX`, `int GridY`, `GameObject VisualObj`, `float Cooldown`.
    - `MissileInFlight` sınıfı (Uçmakta olan Atom / Hidrojen bombası veya Savaş Gemisi mermisi):
      - `StructureType BombType` (`AtomBomb` veya `HydrogenBomb`), `ushort SenderId`, `Vector3 StartPos`, `Vector3 TargetPos`, `float Progress` (`0f..1f`), `LineRenderer TrajectoryLine`, `Transform MissileIcon`.
    - `StructureAndNukeManager` (`MonoBehaviour`):
      - **Bina İnşası (`TryBuildOrExecuteAction(ushort nationId, StructureType actionType, int gx, int gy, out string feedbackMsg)`):**
        - Altın kontrolü yapar (`Gold >= Cost`).
        - Eğer bina ise (`City`, `Port`, `Factory`, `DefensePost`, `SAMLauncher`, `MissileSilo`): Tıklanan hücrenin `nationId`'ye ait olup olmadığını (`Port` için kıyıya `IsCoast` yakınlığını) kontrol eder, altını düşer, sahnede prosedürel 2D bina ikonu (`SpriteRenderer` + koyu dairesel rozet, tıpkı görseldeki SnickTrix'in içindeki bina rozeti gibi!) oluşturur ve ulusun bina sayısını artırır.
        - Eğer `Warship` ise: Kıyıdaki deniz hücresine bir Savaş Gemisi spawn eder; gemi düşman kıyılarına yaklaşarak otomatik top atışı yapar ve kıyı piksellerini tarafsızlaştırır.
        - Eğer `AtomBomb` veya `HydrogenBomb` ise: Ulusun en az 1 `MissileSilo`'su varsa (veya doğrudan fırlatılabilir moddaysa) füzeyi parabolik bir yörünge çizgisiyle (`LineRenderer`) hedefe gönderir.
          - Hedefin yakınında düşmana ait aktif bir `SAMLauncher` varsa (`Range = 45` grid hücresi, `Cooldown <= 0`), SAM bir önleme füzesi ateşleyip nükleer bombayı havada imha eder!
          - İmha edilmezse hedefe düştüğünde: Parlak turuncu/beyaz şok dalgası efekti çıkarır, `Radius` (`Atom = 14`, `Hydrogen = 30`) içindeki tüm pikselleri `TerrainType.Scorched` ve `OwnerId = 0` yapar, içindeki binaları yıkar ve hedef ulusun askerlerini ağır oranda azaltır.
        - Eğer `Alliance` ise: Tıklanan yabancı ulusla ittifak (`Allies`) kurar.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 2.4: 87 AI Ulusu ve Kabile Yapay Zekası (`Assets/Scripts/Nations/AIController.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/AIController.cs`
  - **Yapılacaklar:**
    - `AIController` (`MonoBehaviour`):
      - Her saniye AI uluslarını gruplar halinde (performansı korumak için tick başına 8-10 AI ulusu) değerlendirir:
        - Kabileler (`IsTribe == true`): Sadece savunmada kalır ve yavaşça asker biriktirir, nadiren yanındaki boş araziye genişler.
        - 87 AI Ulusu (`IsTribe == false`):
          - Önce çevresindeki sahipsiz (`OwnerId == 0`) topraklara ve zayıf kabilelere (`%20 - %35` asker oranıyla) saldırarak genişler.
          - Asker doluluk oranı `%60`'ı geçtiğinde komşu uluslardan kendisinden daha az askeri olanlara saldırır (Başlangıç koruma süresi `ProtectionTimer > 0` iken oyuncuya saldırmazlar!).
          - Altını biriktiğinde kendi topraklarına otomatik olarak `City`, `Factory`, `DefensePost`, `Port` ve `MissileSilo` inşa eder; çok güçlenen AI ulusları rakiplerine `AtomBomb` atabilir!
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

---

## FAZ 3: Prosedürel İkon Üreticisi, Dünya Etiketleri ve RoWorld Birebir UI
- [x] **Görev 3.1: Prosedürel Piksel/Vektör İkon ve Sprite Üreticisi (`Assets/Scripts/UI/ProceduralSpriteFactory.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/UI/ProceduralSpriteFactory.cs`
  - **Yapılacaklar:**
    - Dışarıdan hiçbir resim dosyasına ihtiyaç duymadan çalışma zamanında (`Texture2D` üzerine matematiksel şekiller çizerek) şu `Sprite`'ları üreten statik fabrika sınıfı:
      - `GetRoundedRectSprite(int width, int height, int radius)`: Koyu yarı saydam RoWorld UI panelleri için yumuşak köşeli 9-slice / standart sprite.
      - `GetCircleSprite(int size, bool ringOnly)`: Yuvarlak butonlar, bina rozetleri ve seçim halkaları için.
      - **Bina ve Aksiyon İkonları (`GetActionIcon(StructureType type)` - 32x32 piksel net ikonlar):**
        1. `City`: Gökdelen / şehir silüeti ikonu.
        2. `Port`: Çapa / gemi limanı ikonu.
        3. `Factory`: Dişli / tüten fabrika bacası ikonu.
        4. `DefensePost`: Kalkan / kale burcu ikonu.
        5. `SAMLauncher`: Yukarı bakan uçaksavar füze rampası ikonu.
        6. `MissileSilo`: Füze silosu ikonu.
        7. `AtomBomb`: Radyasyon / atom bombası ikonu.
        8. `HydrogenBomb`: Mantar bulutu / H-Bomb ikonu.
        9. `Warship`: Savaş gemisi silüeti ikonu.
        10. `Alliance`: El sıkışma / barış bayrağı ikonu.
      - **Üst ve Alt Bar İkonları:** Taç (`Crown`), Kalkan (`Shield`), Asker/Kask (`Troop`), Altın Sikke (`GoldCoin`), Çapraz Kılıç (`CrossedSwords`), Müzik Notası (`Music`), Dişli Ayarlar (`Gear`), Duraklat (`Pause`), Ses (`Volume`).
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 3.2: Harita Üzeri Dinamik Ülke İsimleri ve Asker Sayıları (`Assets/Scripts/UI/WorldLabelsController.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/UI/WorldLabelsController.cs`
  - **Yapılacaklar:**
    - Görseldeki gibi her ülkenin/kabilenin kendi sınırlarının ortasında (`CenterGridPos`) dinamik büyüyüp küçülen Dünya Uzayı (`WorldSpace`) metinleri (`TextMeshPro`):
      - Üst satır: Ülke/Kabile adı (Örn: **India**, **Oman**, **SnickTrix**, **Fernhold**, **Yak Tribe**). Oyuncunun isminin solunda sarı bir Taç (`Crown`) ikonu bulunur!
      - Alt satır: Mevcut asker sayısı (Örn: **6.28K**, **6.43K**, **8.09K**, **2.40K**).
      - Font boyutu (`localScale`), ülkenin sahip olduğu `TileCount` karekökü ile orantılıdır (Büyük imparatorlukların ismi **India / Oman** gibi devasa görünürken, küçük kabilelerin ismi küçük görünür; kameranın zoom seviyesine göre aşırı küçük kabile yazıları otomatik gizlenerek görüntü kirliliği önlenir).
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 3.3: RoWorld Birebir HUD - Üst Barlar ve Alt Kontrol Paneli (`Assets/Scripts/UI/RoWorldHUDController.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/UI/RoWorldHUDController.cs`
  - **Yapılacaklar:**
    - Tamamen C# koduyla (`Canvas`, `CanvasScaler` 1920x1080 Reference Resolution, `RectTransform`, `Image`, `Button`, `TextMeshProUGUI`) RoWorld ekran görüntüsünün **birebir** kopyası olan koyu lacivert/antrasit (`#12161F` `%90` opaklık) arayüzü inşa et:
    - **1. Sol Üst Yuvarlak İkon Dizisi:**
      - 6 adet küçük yuvarlak koyu buton: Müzik aç/kapa, Hedef/Ortalama (Oyuncu başkentine odakla), İstatistik/Sıralama, İttifaklar, Yeniden Başlat, Ayarlar.
    - **2. Üst Orta "PROTECTED & WORLD CONTROL" Hap (Pill) Paneli:**
      - Sol kutu: Yeşil kalkan ikonu + `PROTECTED` başlığı + geri sayan yeşil sayaç (`2:37` -> `0:00`).
      - Sağ geniş kutu: `WORLD CONTROL` başlığı + sağ üstte sarı `GOAL 80%` etiketi + çok renkli ilerleme çubuğu (Oyuncunun rengi + lider AI ulusların rengi) + altında iki sütun: Sol (`You 0.18%`), Sağ (`#1 India 0.35%`).
    - **3. Sağ Üst Sıralama Rozeti (Rank Pill):**
      - Kupa/Sıralama ikonu + `#290 • 0.18%` (Oyuncunun anlık sıralaması ve dünya yüzdesi).
    - **4. Alt Ana Kontrol Barı (Bottom Dock Bar):**
      - **En Sol:** `||` (Oyunu Duraklat/Hızlandır `1x/2x/4x` butonu) + Oyuncu bayrağı/taç + `SnickTrix` + altında `0.18% of the world`.
      - **TROOPS Bölümü:** `TROOPS` başlığı + sağ üstte yeşil artış hızı (`+140/s`) + altta büyük beyaz rakamla mevcut asker / kapasite (`7.47K / 20.5K`) + doluluk barı.
      - **GOLD Bölümü:** `GOLD` başlığı + sağ üstte yeşil artış hızı (`+1.26K/s`) + sarı altın ikonu + büyük sarı rakamla mevcut altın (`775K`).
      - **ATTACK Oran Seçici Bölümü:** Kılıç ikonu `ATTACK` + sağ üstte seçili yüzde (`20%`) + ortada `10%`, `20%`, `35%`, `50%`, `65%`, `80%`, `100%` hızlı seçim butonları ve kaydırıcı + en altta `Sends 1.49K troops` açıklaması.
      - **Sağdaki 10'lu İnşa ve Nükleer Aksiyon Izgarası (Action Buttons):**
        - Yan yana 10 adet koyu kare buton, her birinin ortasında prosedürel ikonu, altında sarı/turuncu maliyet etiketi (`250K`, `125K`, `125K`, `50K`, `1.5M`, `1M`, `750K`, `5M`, `250K`, `Ally`), üstüne gelince veya seçilince hangi bina/silah olduğunu gösteren bilgi etiketi ve ses aç/kapa (`Mute`) ikonu.
        - Oyuncunun altını yetiyorsa maliyet yazısı parlak sarı (`#F6E05E`), yetmiyorsa soluk kırmızımsı/gri görünür. Seçili aksiyon butonunun çerçevesi parlak mavi/altın rengi yanar.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

---

## FAZ 4: Oyuncu Etkileşimi, Sahne Bootstrapper ve Canlı Doğrulama
- [x] **Görev 4.1: Oyuncu Tıklama, Saldırı/İnşa Etkileşimi ve Oyun Yöneticisi (`Assets/Scripts/Core/GameManagerAndBootstrapper.cs`)**
  - **Dosya Yolu:** `Assets/Scripts/Core/GameManagerAndBootstrapper.cs`
  - **Yapılacaklar:**
    - `GameManager` (`MonoBehaviour`):
      - Tüm alt sistemleri (`WorldMapGrid`, `MapRenderer`, `CameraController2D`, `TerritoryAndCombatManager`, `StructureAndNukeManager`, `AIController`, `WorldLabelsController`, `RoWorldHUDController`) başlatır ve her `0.1f` saniyede bir `Tick()` döngüsünü koşturur.
      - **Sol Tık / Sağ Tık Etkileşimi (UI üzerinde değilken):**
        - Eğer alt bardan bir inşa/nükleer aksiyon seçiliyse (`SelectedAction != StructureType.None`): Tıklanan harita koordinatında (`gx, gy`) o binayı kurar veya nükleer füzeyi ateşler; işlem başarılıysa veya sağ tıklanırsa seçim modundan çıkar.
        - Eğer inşa aksiyonu seçili değilse ve oyuncu haritada kendi ülkesine komşu bir yabancı toprağa/kabilesine veya sahipsiz karaya tıklarsa: Seçili `AttackRatio` (`%20` vb.) ile anında o bölgeye saldırı (`StartAttack`) başlatır!
      - **Zafer / Yenilgi Ekranı:** Herhangi bir ulus `%80` dünya kontrolüne ulaştığında veya oyuncunun tüm toprağı yok olduğunda "VICTORY / DEFEAT - Play Again" penceresi gösterir.
    - `GameBootstrapper` (`static class`):
      - `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` ile sahne yüklendiğinde sahnede `GameManager` yoksa otomatik oluşturur, kamerayı ayarlar ve oyunu başlatır.
      - Ayrıca Editor menüsüne (`[MenuItem("MeWorld/Setup Scene Objects")]`) sahneye kalıcı `MeWorld_GameManager` objesini ekleyen bir komut koyar.
  - **Kabul Kriterleri:** `unity recompile` sıfır hata ile geçmeli.

- [x] **Görev 4.2: Sahne Kurulumu, Play Mode Testi ve Görsel/Konsol Doğrulaması**
  - **Yapılacaklar:**
    - `unity command` ile sahnede `GameBootstrapper` kurulumunu tetikle ve sahneyi kaydet.
    - Play Mode'u başlat, birkaç saniye ilerlet, konsolda hiçbir `Error` veya `Exception` olmadığını doğrula ve ekran görüntüsü alarak UI ile haritanın RoWorld görselindeki gibi çalıştığını teyit et.
  - **Kabul Kriterleri:** Sıfır konsol hatası, eksiksiz çalışan RoWorld klonu.

---

## FAZ 5: QE Hata Düzeltmeleri ve RoWorld Birebir İyileştirmeleri
- [x] **Görev 5.1: Haritanın Voronoi / Bölgesel Tiling ile Boşluksuz Hale Getirilmesi (BUG-07)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/TerritoryAndCombatManager.cs`
  - **Yapılacaklar:**
    - Ülkeleri sadece daire fırçalarla izole bırakmak yerine, başlangıç merkezlerinden dışarıya doğru eşzamanlı BFS (Multi-source Breadth-First-Search / Voronoi diagram) yayılımı uygula.
    - Haritadaki tüm kara parçaları uluslar ve kabileler arasında organik bir şekilde paylaşılsın; ülkelerin arasında yapay boşluklar/delikler kalmasın.
    - Sınır hesaplaması (`UpdateAllBorders`) ile tüm ülkeler RoWorld'deki gibi birbirine komşu olsun.
  - **Kabul Kriterleri:** `unity recompile` hatasız geçmeli, haritada boş kara kalmamalı.

- [x] **Görev 5.2: Savaş Motoru Optimizasyonu & Cephe Odaklı Fetih (BUG-01 & BUG-02)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/TerritoryAndCombatManager.cs`
  - **Yapılacaklar:**
    - `TickCombat()` içindeki `O(W * H)` tam harita döngüsünü kaldır.
    - Her `AttackWave` için cephe piksellerini hedef ve saldıran temas sınırlarından toplayıp, tıklanan `clickGridX, clickGridY` cephesine olan mesafeye göre (`Distance-squared`) sırala.
    - Fetih tam olarak oyuncunun tıkladığı cepheden içeriye doğru gerçekleşsin.
  - **Kabul Kriterleri:** `unity recompile` hatasız geçmeli, CPU yükü düşmeli ve saldırı tıklanan cepheden yayılmalı.

- [x] **Görev 5.3: Nükleer Silah Fırlatma Kuralları & SAM Launcher Hedef Kontrolü (BUG-03 & BUG-04)**
  - **Dosya Yolu:** `Assets/Scripts/Structures/StructureAndNukeManager.cs`
  - **Yapılacaklar:**
    - `AtomBomb` ve `HydrogenBomb` fırlatılmadan önce ulusun en az 1 aktif `MissileSilo` binasına sahip olup olmadığını denetle (`nation.SiloCount > 0`). Silo yoksa oyuncuya/AI'a "Requires an active Missile Silo!" geri bildirimi ver.
    - `UpdateMissiles()` içinde SAM Launcher füzeyi havada önlerken, sadece hedefin (`TargetGridX/Y`) o SAM'in sahibinin veya müttefikinin sınırları içinde olup olmadığını kontrol et. Alakasız 3. taraf ülkelerin SAM'leri füzeyi engellemesin.
  - **Kabul Kriterleri:** `unity recompile` hatasız geçmeli.

- [x] **Görev 5.4: Ekonomi ve Büyüme Hızlarının Orijinal RoWorld Seviyesine Kalibrasyonu (BUG-05)**
  - **Dosya Yolu:** `Assets/Scripts/Nations/TerritoryAndCombatManager.cs` & `Assets/Scripts/Core/GameConfig.cs`
  - **Yapılacaklar:**
    - Başlangıçta SnickTrix için asker büyümesini `+140/s`, altın gelirini `+1.26K/s`, asker kapasitesini `~20.5K - 23.9K` civarına getirecek formül katsayılarını kalibre et.
    - Lojistik büyüme eğrisini ve bina çarpanlarını oyun hızını dengeli tutacak şekilde ayarla.
  - **Kabul Kriterleri:** `unity recompile` hatasız geçmeli, başlangıç barları görseldeki oranlarla uyuşmalı.

- [x] **Görev 5.5: Sol Üst Butonların Piktogram İkonlara Çevrilmesi ve Başkent/Kabile Yazı Hiyerarşisi (BUG-06 & Görsel Cila)**
  - **Dosya Yolu:** `Assets/Scripts/UI/ProceduralSpriteFactory.cs`, `Assets/Scripts/UI/RoWorldHUDController.cs`, `Assets/Scripts/UI/WorldLabelsController.cs`
  - **Yapılacaklar:**
    - Sol üstteki 6 yuvarlak buton için metin kısaltmaları (`SND`, `HQ`, `TOP`, `ALLY`, `RST`, `CFG`) yerine saf vektörel piktogram ikonlar (`GetTopBarIcon(int index)`) çiz ve bağla.
    - Sağ üstteki rozeti orijinaldeki gibi `🏆 #210 • X.XX%` formatına getir.
    - Harita üzerindeki kabile isimlerini (`IsTribe == true`) daha küçük (`fontSize * 0.7f`) ve soluk gri tonda göstererek büyük imparatorlukların (`SnickTrix`, `India`, `Oman`) öne çıkmasını sağla.
  - **Kabul Kriterleri:** `unity recompile` hatasız geçmeli, UI referans görüntüyle tam örtüşmeli.

- [x] **Görev 5.6: Test, Derleme, Canlı Play Mode Doğrulaması ve Nihai Ekran Görüntüsü**
  - **Yapılacaklar:**
    - Unity Play Mode'da tüm düzeltmeleri test et, konsol hata/uyarı loglarını kontrol et ve son ekran görüntüsünü yakala.
  - **Kabul Kriterleri:** Sıfır konsol hatası ve kusursuz çalışan RoWorld deneyimi.
