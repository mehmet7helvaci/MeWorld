# MeWorld - Zorunlu Ajan Kuralları

Bu çalışma alanında (`MeWorld`) çalışan her yapay zeka ajanı, herhangi bir işlem yapmadan önce kök dizindeki `@[AGENTS.md](AGENTS.md)` ve `@[PIPELINE.md](PIPELINE.md)` dosyalarını okumak ve içindeki kurallara harfiyen uymak zorundadır.

- Kullanıcı **"devam et"** dediğinde `PIPELINE.md` dosyasındaki ilk `[ ]` veya `[~]` işaretli görevi bul, kodla, `unity recompile` ile derle, hatasız olduğunu doğrula ve `[x]` olarak işaretleyip bir sonraki göreve geç.
- Asla dışarıdan hazır görsel/prefab bekleme; tüm harita, ikon ve UI elemanlarını C# koduyla prosedürel olarak üret.
- Asla projede yüklü olmayan (`DOTween`, `UniTask` vb.) kütüphaneleri kullanma.
