# LOS SANTOS INTERNET RADIO LS
## Kullanıcı Kılavuzu - v1.0.2 BETA TEST

**Dil:** Türkçe  
**Test edilen temel:** v7.41  
**Kullanım:** GTA V Singleplayer

> **Önemli:** Bu mod yalnızca GTA V Singleplayer için tasarlanmıştır. Online/ağ oturumu algılanırsa mod işlevleri devre dışı bırakılır.

## İçindekiler

1. Hızlı Başlangıç
2. Kontroller
3. HOME ve Ses Kaynakları
4. Internet Radio
5. Spotify ve YouTube Music
6. Favoriler
7. CAR / Araç Menüsü
8. Işıklar ve Plaka Lambası
9. Turbo Blow-Off
10. HUD, Hız Göstergesi ve Araç Bilgileri
11. Ayarlar ve Kaydetme
12. Kurulum, Güncelleme ve Kaldırma
13. Sorun Giderme
14. Beta Test: Neler Bildirilmeli?

---

## 1. Hızlı Başlangıç

1. **ScriptHookV** ve **ScriptHookVDotNet** kurun.
2. Paketteki `scripts` klasörünün tamamını GTA V ana klasörüne kopyalayın.
3. GTA V'yi çalıştıran Windows hesabının `GTA V/scripts/InternetRadio/` klasöründe **okuma/yazma/değiştirme** iznine sahip olduğundan emin olun.
4. GTA V'yi **Singleplayer** modunda başlatın.
5. Desteklenen bir araca binin.
6. Multimedya menüsünü açmak için `NUM0` tuşuna basın.
7. `NUM8 / NUM2` ile gezinip `NUM5` ile seçin.

İlk kullanımda `scripts/InternetRadio/UserSettings.ini` otomatik olarak oluşturulur. Kişisel ayarlar ana yapılandırmadan ayrı olarak burada saklanır.

### Bu beta için referans kurulum

- **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6)**
- **API 3.9.0**
- GTA V Enhanced / Singleplayer

Diğer uyumlu SHVDN sürümleri de çalışabilir. Derleme hatası alırsanız önce `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll` ve `ScriptHookVDotNet3.dll` dosyalarının **aynı sürüm paketinden** geldiğini kontrol edin.

## 2. Kontroller

| Tuş | İşlev |
|---|---|
| `NUM0` | Multimedya menüsünü aç / kapat |
| `NUM8 / NUM2` | Seçimi yukarı/aşağı taşı; Spotify/YouTube'da önceki/sonraki parça |
| `NUM4 / NUM6` | Sekme değiştir; menü dışında önceki/sonraki istasyon/kaynak |
| `NUM5` | Seç / uygula / kaynağı etkinleştir |
| `NUM1` | Aktif kaynağı aç/kapat veya Play/Pause |
| `NUM3` | Aktif kaynağı durdur/duraklat |
| `NUM- / NUM+` | Sesi azalt / artır |
| `NUM7` | Dörtlü flaşör aç/kapat |
| `NUM9` | Manuel uzun far aç/kapat |
| `SPACE` | İstasyonu favorilere ekle/çıkar |
| `DELETE` | Seçili favoriyi sil |
| `F8` | Yapılandırmayı yeniden yükle |
| `ESC / BACK` | Menüyü kapat |

`Num Lock` açık olmalıdır.

## 3. HOME ve Ses Kaynakları

HOME sistemin merkezidir. Buradan Internet Radio, GTA Radio, YouTube Music, Spotify, CAR/Araç, skinler, ayarlar ve bilgi bölümlerine ulaşabilirsiniz.

Son aktif kaynak, araç değişiminden veya yeniden başlatmadan sonra mümkün olduğunda geri yüklenir. Internet Radio, GTA Radio, Spotify ve YouTube Music ayrı ayrı takip edilir.

## 4. Internet Radio

İstasyonları kategoriye göre seçebilirsiniz. Yayın metaveri sağlıyorsa parça ve sanatçı bilgisi gösterilir.

Yayınlar üçüncü taraflar tarafından işletilir. Bir istasyon moddan bağımsız olarak çevrimdışı olabilir, URL'sini değiştirebilir veya bölgesel olarak engellenebilir. Bir istasyon çalışmıyorsa önce başka bir istasyon deneyin.

## 5. Spotify ve YouTube Music

Spotify ve YouTube Music, mevcut Windows/uygulama medya oturumunu kullanır.

1. Spotify veya YouTube Music'i açın.
2. Bir parça başlatın.
3. LS multimedya menüsünde ilgili sekmeyi açın.
4. Kaynağı etkinleştirmek için `NUM5` tuşuna basın.
5. `NUM1` = Play/Pause, `NUM8 / NUM2` = önceki/sonraki parça.

Sağ panel bağlantı durumunu ve uygulama ses seviyesini gösterir. **BAĞLI DEĞİL** ifadesi uygun bir medya oturumunun algılanmadığını gösterir.

## 6. Favoriler

Bir istasyonda `SPACE` tuşuna basarak favorilere ekleyebilir veya çıkarabilirsiniz. En fazla **6 favori** desteklenir. `DELETE` seçili favoriyi siler.

Favoriler kullanıcı ayarlarında saklanır ve yeniden başlatmadan sonra korunur.

## 7. CAR / Araç Menüsü

**CAR / ARAÇ** sekmesinde araca bağlı olarak isteğe bağlı konfor, aydınlatma ve gösterge özellikleri bulunur:

- Otomatik sinyaller
- `NUM7` ile dörtlü flaşör
- `NUM9` ile uzun far
- Beat Neon
- Kabin ışığı
- Plaka lambası
- Turbo Blow-Off
- Hız göstergesi / Mini HUD
- Üretici logosu
- RPM ve araç göstergeleri

Bazı özel araç sınıfları belirli özellikleri bilinçli olarak kullanmayabilir.

## 8. Işıklar ve Plaka Lambası

v1.0.2 Beta Test, test edilmiş v7.41 plaka lambası mantığını kullanır:

- Normal gündüz koşullarında plaka lambası **KAPALI** kalır.
- Gün ışığı, gölgeler, DRL veya GTA'nın otomatik ışık durumları tek başına lambayı açmaz.
- Sürücünün gerçek bir far komutu gündüz etkinleştirebilir.
- Gece gerçek kısa/uzun farları takip eder.
- Kısa süreli durum değişimleri titremeyi önlemek için filtrelenir.
- CAR menüsünde `PLATE LIGHT` açık olmalıdır.

Sabit neon plaka lambasının rengini belirleyebilir. Sabit neon yoksa normal/xenon far renk ailesi kullanılır. Beat Neon plaka lambasını nabız gibi yanıp söndürmez.

## 9. Turbo Blow-Off

**Turbo Blow-Off** bilinçli olarak yalnızca `ON / OFF` şeklindedir.

Açık olduğunda gerçek turbo yükünden sonra tek bir güçlü SPORT tarzı blow-off efekti tetiklenir:

- vites yükseltirken veya
- boost sonrasında gaz belirgin şekilde bırakıldığında.

Anti-spam mantığı tekrarlanan efektleri önler. Algılama yüksek hızda ve araç havadayken de çalışır. Ek add-on modeller `InternetRadio.ini` içindeki `FactoryTurboModels=` satırına eklenebilir.

## 10. HUD, Hız Göstergesi ve Araç Bilgileri

Araç sınıfına göre farklı göstergeler kullanılır:

- Kara araçları: hız / RPM / araç verileri
- Tekneler: deniz HUD'u
- Uçaklar ve helikopterler: uçuş HUD'u

Hız değeri GTA'nın gerçek araç hızını kullanır. Üretici logoları GTA'nın araç HUD dokularından yüklenir; uygun logo yoksa nötr bir simge gösterilir.

## 11. Ayarlar ve Kaydetme

Kişisel ayarlar otomatik olarak şu dosyaya kaydedilir:

`GTA V/scripts/InternetRadio/UserSettings.ini`

Buna birçok UI, ses, araç, favori, son kaynak ve son istasyon ayarı dahildir.

`InternetRadio.ini` ana yapılandırmayı, istasyon listesini, teknik varsayılanları ve isteğe bağlı add-on ayarlarını içerir. Güncellemeden önce elle yaptığınız değişiklikleri yedekleyin.

## 12. Kurulum, Güncelleme ve Kaldırma

### Yeni kurulum

Beta paketindeki `scripts` klasörünün tamamını GTA V ana klasörüne kopyalayın.

### Güncelleme

1. GTA V'yi kapatın.
2. İsterseniz `scripts/InternetRadio/UserSettings.ini` dosyasını yedekleyin.
3. Yeni `scripts` dosyalarını eskilerin üzerine kopyalayın.
4. Kişisel ayarları korumak için `UserSettings.ini` dosyasını saklayın.
5. GTA V'yi başlatıp modu test edin.

### Kaldırma

`scripts/03_InternetRadioSimple.3.cs` ve `scripts/InternetRadio/` öğelerini kaldırın. `scripts` klasöründeki diğer modları silmeyin.

## 13. Sorun Giderme

**Menü açılmıyor:** ScriptHookV, ScriptHookVDotNet, Singleplayer modu ve desteklenen bir araçta olduğunuzu kontrol edin.

**C# derleme hatası:** `ScriptHookVDotNet.log` dosyasını açın, SHVDN sürümünü kontrol edin ve tüm SHVDN dosyalarının aynı paketten geldiğinden emin olun. Test edilmiş referans: **ScriptHookV .NET Enhanced 3.9.0.6 (1.1.0.6), API 3.9.0**.

**Radyo çalmıyor:** başka bir istasyon deneyin; üçüncü taraf yayınlar çevrimdışı veya bölgesel olarak engelli olabilir.

**Spotify/YouTube BAĞLI DEĞİL gösteriyor:** önce uygulamada/tarayıcıda oynatmayı başlatın ve Windows'un medya oturumu algıladığını kontrol edin.

**Ayarlar kaydedilmiyor:** `GTA V/scripts/InternetRadio/` için okuma/yazma/değiştirme izinlerini kontrol edin.

**Plaka lambası yanmıyor:** `PLATE LIGHT` ayarını açın ve aracın gerçek farlarını yakın.

**Plaka lambası gündüz yanıyor veya titriyor:** araç adı, saat, far konumu ve mümkünse kısa bir video ile bildirin.

**INI değişiklikleri etkili olmuyor:** `F8` tuşuna basın veya GTA V'yi yeniden başlatın.

## 14. Beta Test: Neler Bildirilmeli?

Mümkünse şunları ekleyin:

- GTA V **Enhanced veya Legacy**
- ScriptHookVDotNet sürümü
- araç adı / add-on spawn adı
- aktif kaynak
- aktif ayar
- hatayı tekrarlamak için kesin adımlar
- UI/aydınlatma sorunları için ekran görüntüsü veya kısa video
- `ScriptHookVDotNet.log` içinden ilgili satırlar

v1.0.2 için öncelikli alanlar: sağ UI yerleşimi, plaka lambası, Turbo Blow-Off, kullanıcı ayarlarının kalıcılığı ve Spotify/YouTube bağlantı durumu.

---

**LOS SANTOS INTERNET RADIO LS v1.0.2 BETA TEST**  
GTA V Singleplayer için resmi olmayan modifikasyon.
