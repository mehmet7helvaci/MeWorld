# MeWorld (RoWorld / OpenFront Clone) - Agent Operating Rules & Anti-Hallucination Directives

> **DİKKAT: BU DOSYA PROJEDE ÇALIŞAN TÜM YAPAY ZEKA AJANLARI İÇİN ZORUNLU ANAYASADIR.**
> Kullanıcı sadece **"devam et"** dediğinde bile aşağıdaki protokole harfiyen uymak ZORUNDASIN.

---

## 1. "Devam Et" (Handoff & Resume) Protokolü

Kullanıcı **"devam et"** (veya benzeri bir sürdürme komutu) verdiğinde **ASLA** kullanıcıya "Nereden devam edeyim?" diye sorma ve **ASLA** rastgele kod yazmaya başlama! Tam olarak şu sırayı izle:

1. **Görev Ağacını Oku:** `c:\Users\mehme\OneDrive\Masaüstü\MeWorld\PIPELINE.md` dosyasını `read_file` ile oku.
2. **Sıradaki Görevi Bul:** `PIPELINE.md` içindeki `[ ]` (henüz tamamlanmamış) olan **İLK** alt görevi (Task) tespit et (`[~]` varsa yarım kalmış demektir, önce onu tamamla).
3. **Mevcut Kodları Kontrol Et:** O görevle ilgili dosyaların diskte var olup olmadığını veya ne durumda olduğunu `read_file` ile kontrol et.
4. **Kodu Yaz ve Derle:**
   - İlgili C# scriptlerini `Assets/Scripts/...` altına yaz veya güncelle.
   - Her dosya yazımından sonra **MUTLAKA** derleme ve konsol kontrolü yap:
     ```powershell
     unity recompile --project-path "c:\Users\mehme\OneDrive\Masaüstü\MeWorld" --format json
     ```
5. **Doğrula ve İşaretle:**
   - Derleme hatası (`compileErrors`) veya uyarı varsa **anında** çöz.
   - Görevin "Kabul Kriterleri (Acceptance Criteria)" maddelerinin tamamlandığından emin ol.
   - `PIPELINE.md` dosyasındaki ilgili görevi `[x]` olarak güncelle (`edit_file`).
6. **Durmadan İlerle:** Token kotan veya tur limitin bitene kadar bir sonraki `[ ]` görevine geçerek üretmeye devam et.

---

## 2. Anti-Halüsinasyon ve "Yapay Zeka Hastalıklarını" Önleme Kuralları

En düşük kapasiteli model bile olsan şu hataları **YAPamazsın**:

1. **Olmayan API / Paket Uydurma Yasağı (Zero API Hallucination):**
   - Projede **SADECE** `Packages/manifest.json` içinde yüklü paketler kullanılabilir:
     - `UnityEngine`, `UnityEngine.UI`, `TMPro` (TextMeshPro `com.unity.ugui` 2.0.0 içinde gömülüdür).
     - `UnityEngine.InputSystem` (Yeni Input System aktif! Eski `Input.GetMouseButtonDown` veya `Input.mouseScrollDelta` yerine `UnityEngine.InputSystem.Mouse.current` ve `Keyboard.current` kullan veya her ikisini destekleyen güvenli sarmalayıcı yaz).
     - `UnityEngine.Rendering.Universal` (URP 2D).
   - **YASAK:** `DOTween`, `UniTask`, `OdinInspector`, `Mirror`, `Netcode` gibi projede yüklü OLMAYAN 3. parti kütüphaneleri `using` ile çağırmak **KESİNLİKLE YASAKTIR**. Tüm animasyon ve geçişleri saf C# `Coroutine` veya `Update` içinde `Mathf.Lerp` / `Mathf.SmoothDamp` ile yaz.
2. **Hayali Asset / Prefab / Sprite Referansı Yasağı (Zero Missing Reference):**
   - Kullanıcıdan dışarıdan PNG, 3D model, ses veya Prefab bekleme!
   - `Resources.Load("BilinmeyenDosya")` kullanarak çalışma zamanında `NullReferenceException` üretme!
   - Tüm harita dokuları (`Texture2D`), ikonlar (`Sprite`), renk paletleri ve UI bileşenleri **Runtime'da veya Editor scriptleriyle %100 prosedürel (kod ile piksel piksel çizilerek)** üretilmelidir.
3. **Yarım Kod / Placeholder ("// TODO") Yasağı:**
   - Kod içinde `// TODO: implement later`, `// ... rest of code` gibi eksik bloklar bırakmak **KESİNLİKLE YASAKTIR**.
   - Her sınıf ve metot ilk yazıldığında tam, çalışır ve uçtan uca bağlı olmak zorundadır.
4. **Sahne ve YAML Dosyalarını Elle Bozma Yasağı:**
   - `.unity`, `.prefab`, `.asset` dosyalarını metin editörüyle (`write_file` / `edit_file`) elle düzenleme.
   - Sahne kurulumunu ya `unity command` CLI araçlarıyla ya da `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` kullanan bir `GameBootstrapper` C# sınıfı üzerinden otomatik inşa et. Böylece sahne (`SampleScene`) açıldığı anda veya Play tuşuna basıldığı anda hiçbir manuel Inspector atamasına gerek kalmadan tüm oyun dünyası ve UI eksiksiz oluşur!
5. **Körü Körüne İlerleme Yasağı:**
   - Bir script yazdığında derlenip derlenmediğini görmeden ikinci scripte geçme. Her adımda `unity recompile` veya `unity status --format json` ile Safe Mode / derleme hatası olup olmadığını kontrol et.

---

## 3. Teknik Mimari ve Kodlama Standartları (RoWorld / OpenFront Clone)

Proje, görselde belirtilen **RoWorld [World PvP] (OpenFront tabanlı)** gerçek zamanlı strateji ve toprak fetih oyununun tek oyunculu / 88 AI uluslu + yüzlerce kabileli (Tribe) tam teşekküllü Unity klonudur.

### A. Klasör ve Namespace Yapısı
Tüm C# kodları `Assets/Scripts/` altında, `MeWorld` ana namespace'i altında toplanmalıdır:
- `Assets/Scripts/Core/` -> `GameBootstrapper.cs`, `GameConfig.cs`, `GameStateManager.cs`, `TickManager.cs`
- `Assets/Scripts/Map/` -> `WorldMapGenerator.cs`, `MapGrid.cs`, `TerritoryManager.cs`, `MapRenderer.cs`, `CameraController2D.cs`
- `Assets/Scripts/Nations/` -> `NationData.cs`, `NationManager.cs`, `CombatResolver.cs`, `DiplomacyManager.cs`, `AIController.cs`
- `Assets/Scripts/Structures/` -> `StructureType.cs`, `StructureData.cs`, `StructureManager.cs`, `NukeSystem.cs`
- `Assets/Scripts/UI/` -> `UIFactory.cs`, `ProceduralIconGenerator.cs`, `TopHUDController.cs`, `BottomControlBarController.cs`, `WorldLabelsController.cs`, `RadialContextMenu.cs`

### B. Harita ve Toprak Sistemi (Grid & Pixel-Buffer Mimarisi)
- OpenFront / RoWorld tarzı piksel-bölge fetih mekaniği için yüksek performanslı 2D Grid (`Width = 512`, `Height = 288` veya `600x340`) kullanılmalıdır.
- Her hücre (`cellIndex = y * Width + x`):
  - `TerrainType`: `DeepOcean`, `ShallowWater`, `Plains`, `Highlands`, `Mountains`.
  - `OwnerId`: `ushort` (`0` = Tarafsız/Deniz, `1` = Oyuncu, `2..89` = 88 AI Ulusu, `90..220` = Kabileler / Tribes).
  - `IsBorder`: Komşu 4 hücreden en az biri farklı `OwnerId`'ye sahipse `true`.
- **Görselleştirme (`MapRenderer`):**
  - `Texture2D` üzerinde `Color32[]` piksel dizisi tutularak `SetPixels32` + `Apply(false)` ile tek bir `SpriteRenderer` üzerinde çizilir (`FilterMode.Point` veya `Bilinear`).
  - Sınır pikselleri (`IsBorder == true`) ulusun ana renginin koyu/parlak tonuyla çizilir, iç pikseller ulusun yarı saydam/pastel rengi ve arazi yüksekliği gölgesiyle harmanlanır. Oyuncunun (`OwnerId == 1`) sınırları görseldeki gibi parlak pembe/macenta (`#FF4D94`) vurgulanır.

### C. Oyun Döngüsü (Tick System) ve Mekanikler
- **Tick Hızı:** Saniyede 10 Tick (`0.1f` sn aralıklarla `OnTick` çalışır).
- **Protected (Koruma) Süresi:** Oyun başında `2:37` (157 saniye) başlangıç koruması sayacı çalışır (Oyuncu saldırı yaparsa erken kalkabilir veya AI'lar oyuncuya bu sürede saldıramaz).
- **Asker (Troops) ve Altın (Gold) Ekonomisi:**
  - Her ulusun sahip olduğu toprak (`TileCount`) ve Şehir (`City`) sayısına göre maksimum asker kapasitesi (`MaxTroops`) ve saniyelik asker artışı (`+Troops/s`) vardır. Lojistik eğrisi: Asker sayısı `%40-%50` doluluktayken büyüme hızı zirvededir, kapasiteye yaklaştıkça yavaşlar.
  - Altın (`Gold`) her tick'te sahip olunan toprak, Şehir (`City`), Liman (`Port`) ve Fabrika (`Factory`) sayısına göre artar (`+Gold/s`).
- **Saldırı (Attack) ve Fetih:**
  - Alt bardaki **Attack Ratio Slider** (`10%`, `20%`, `35%`, `50%`, `65%`, `80%`, `100%`) gönderilecek asker yüzdesini belirler.
  - Komşu bir ulusa/kabileden toprağa tıklandığında seçili yüzde kadar asker bir `AttackWave` (Saldırı Dalgası) olarak ayrılır ve her tick'te hedef ulusun sınır piksellerini savunma gücüne (`DefensePost` varsa savunma bonusu uygulanır) göre eriterek piksel piksel (`BFS / Flood-Frontier`) fetheder.
- **Binalar ve Nükleer Silahlar (10 Alt Bar Aksiyonu):**
  1. **City (Şehir - 250K Gold):** Max nüfus kapasitesini ve gelir hızını artırır.
  2. **Port (Liman - 125K Gold):** Kıyı şeridine kurulur, deniz ticaretiyle yüksek altın üretir ve deniz aşırı saldırı imkanı verir.
  3. **Factory (Fabrika - 125K Gold):** Saniyelik altın üretimini (`+Gold/s`) ciddi oranda katlar.
  4. **Defense Post (Savunma Karakolu - 50K Gold):** Çevresindeki dost sınır piksellerine `%250` savunma direnci kazandırır.
  5. **SAM Launcher (Hava Savunma - 1.5M Gold):** Belirli bir menzildeki düşman Atom ve Hidrojen bombalarını havada imha eder.
  6. **Missile Silo (Füze Silosu - 1M Gold):** Nükleer füze ateşlemek için gereken fırlatma rampası.
  7. **Atom Bomb (Atom Bombası - 750K Gold):** Hedeflenen bölgede orta çaplı (`Radius = 12`) krater açar, binaları yıkar, askerleri yok eder ve toprakları tarafsızlaştırır.
  8. **Hydrogen Bomb (Hidrojen Bombası - 5M Gold):** Devasa çapta (`Radius = 28`) yıkım yapar.
  9. **Warship (Savaş Gemisi - 250K Gold):** Deniz piksellerinde devriye gezer, kıyı bombardımanı yapar.
  10. **Alliance / Diplomacy (İttifak / Barış):** Hedef ulusla saldırmazlık paktı ve ittifak kurar.
- **Zafer Koşulu (Win Condition):** Dünya karalarının **`80%`**'ini kontrol eden ulus oyunu kazanır (`WORLD CONTROL - GOAL 80%`).
