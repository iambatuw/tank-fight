# Kontrol sonuçları — 11 Ekim 2026

## Son güncelleme

- 1.5.0: Kanyon, Liman ve Kutup İstasyonu eklendi; eski üç arena korundu. Yeni haritalarda on özgün, tekrar kullanılabilir düşük poligonlu model bulunuyor.
- Yeni modellerde eksik mesh, malzeme veya desteklenmeyen shader bulunmadı. Model başına 60–660 üçgen ve 2–5 malzeme grubu var; dekoratif parçalar birleştirildi.
- Üç yeni haritanın 12 başlangıç alanı çarpışma açısından ve başlangıç/güçlendirme noktaları arasındaki 84 yol bağlantısı kontrol edildi. Kutup haritasında başlangıca yakın bir kayalık test sırasında taşındı.
- Beş bot modeli × üç yeni harita ve her haritada iki oyunculu mod: toplam 18 çalışma kontrolü geçti. Botlar geçerli rotalarla hareket etti; oyuncular kamera kadrajında kaldı; güçlendirmeler zeminin üzerinde ve erişilebilir doğdu.
- 627 geri sayım örneğinde tank hareketi kapalıydı. Güçlendirme toplandıktan sonra botun savaş rotasına dönmesi sırasında boş rota görülebileceği testte dikkate alındı.
- Altı arena arasında 300 rastgele seçim denendi: tüm arenalar seçildi, aynı arena art arda gelmedi, yeni arena indeksleri ayarlarda korundu.
- Kanyon, Liman ve Kutup İstasyonu görselleri oyun kamerasıyla incelendi; zemin yüzeylerinin üst üste gelmesi giderildi.
- Seri atışın dar simgesi üçlü simgeyle genişletildi: 0,40 yerine 1,40 birim genişlik. Yerdeki güçlendirme ve tank üzerindeki gösterge güncellendi; toplama alanı ve etkisi değişmedi. Diğer güçlendirmelerle yan yana görüntüsü incelendi.
- Raunt göstergesindeki yanıltıcı toplam kaldırıldı; artık dördüncü rauntta “4 / 3” gösterilmiyor.
- 1920 × 1080 ana menüde altı harita adı görüntü üzerinden incelendi; etkin metinlerde taşma ve Unity konsolunda hata bulunmadı. Test için değiştirilen oyun tercihleri geri yüklendi.
- Windows 64 bit 1.5.0 derlemesi başarılı: 0 hata, 180,06 MB. Unity'nin mevcut eski ayar biçimi/paket uyarıları sürüyor (32 uyarı).
- Güncel Windows oyunu 12 saniyelik arka plan açılış kontrolünde çalışır kaldı; taranan çalışma istisnaları bulunmadı. Bu kontrol görsel test değildir; görsel kontroller Unity oyun görünümünde yapıldı.

## 1.4.1 kontrolleri

- Bot modeli artık maç başında bir kez rastgele seçiliyor; raunt içinde yeniden seçilmiyor. Kolayda iki, ortada üç, zorda dört model kullanılabiliyor; can sınırları 35/45/50.
- Üç zorluk için toplam 300 tank seçimi denendi; beklenen modeller geldi ve art arda aynı tank seçilmedi.
- Raunt beklemesi görünür bir geri sayımla gösteriliyor. Bot her yeniden etkinleşmede ilk rotasını beklemeden istiyor ve önce kısa bir hareket yapıyor.
- Devre dışı AI bileşeninin başlatma sırasında referanslarını almaması düzeltildi; hedef rotaları gezinme yüzeyine oturtulup geçerli tam yollarla sınırlandı.
- Beş tank modeli × üç arena için 15 gerçek başlangıç kontrolü geçti: geri sayımda iki tarafın hareketi kapalı kaldı, ardından bot geçerli rota bulup hareket etti. Can sınırları her birleşimde doğrulandı.
- Yenilenen geri sayım 1080p görünümde incelendi; etkin metinlerde taşma veya konsol hatası bulunmadı. Raunt değişiminde seçili bot modeli korundu.
- 1.4.1: Harita yüklemeleri tek seferlik, hedef sahneye bağlı bir geçiş üzerinden yapılıyor. Otomatik başlatma isteği artık kalıcı oyuncu ayarlarında tutulmuyor.
- İki modda toplam altı yeniden oynama geçişi kontrol edildi; Orman, Çöl ve Ay Üssü açıldı.
- 120 tekrarlı yeniden oynama çağrısı ve maç sırasında 480 eski menü/başlatma çağrısı ek sahne yüklemedi, tankları yeniden oluşturmadı.
- Her geçişte tek oyun yöneticisi bulundu; oyun sırasında menü klavye gezinmesi kapalı kaldı.
- Aynı sonuç olayı iki kez gönderildiğinde altın ödülü yalnızca bir kez verildi.
- Duraklatma menüsünden 20 tekrarlı dönüş çağrısı tek ana menü açtı. Ardından 20 tekrarlı başlatma çağrısı tek maç açtı.
- Güçlendirme bildirimi iki satır ve oyuncu rengiyle yenilendi; 1080p önizlemede metin taşması ve rakip işaretiyle çakışma görülmedi.
- Çalışma kontrollerinin sonunda Unity konsolunda hata bulunmadı. Denemelerde kullanılan altın ve oyun tercihleri geri yüklendi.
- 1.4.1 Windows 64 bit derlemesi 0 hatayla tamamlandı (178,12 MB). Mevcut Unity proje/paket uyarıları devam ediyor.
- Oluşturulan Windows EXE'si 12 saniyelik arka plan açılış kontrolünde çalışır kaldı; taranan çalışma istisnaları bulunmadı. Bu kontrol görsel oyun testi değildir; görsel ve maç akışı kontrolleri Unity oyun görünümünde yapıldı.

## 1.4.0 kontrolleri

- Başlık ve kalın yazılardaki gereksiz harf aralığı kaldırıldı; Türkçe karakterler korunuyor.
- Oyuncu, tank, can ve skor göstergeleri yarı şeffaf kartlarla yenilendi; renk şeritleri oyuncu rengine bağlı.
- 1920 × 1080 ana menü, bot maçı ve iki oyunculu maçta etkin metinlerde taşma bulunmadı.
- Rastgele harita seçimi 120 kez denendi: üç arena da seçildi, art arda tekrar oluşmadı.
- Bot maçında Ay Üssü açılışı; iki oyunculu maçta Orman açılışı ve yeniden oynama ile Ay Üssü'ne geçiş doğrulandı.
- Yeni maç haritayı belirliyor; rauntlar haritayı yeniden seçmiyor.
- 1.4.0 Windows 64 bit derlemesi başarılı: 0 hata, 178,12 MB. Unity'nin mevcut proje ayarı ve paket uyarıları devam ediyor.

## Önceki kontroller

- Botlar yakındaki ulaşılabilir güçlendirmeleri hedefleyip topluyor; canları azaldığında iyileştirmeye öncelik veriyor.
- Altı güçlendirme gerçek botta ana ve alt nesne çarpıştırıcılarıyla toplam 12 kez denendi; etkilerin uygulandığı doğrulandı.
- Bot beş birim uzaklıktaki hız güçlendirmesine yürüyerek ulaştı; fizik çarpışmasıyla topladı ve hızı 9'dan 14'e çıktı.
- Tam canla kalkan, az canla iyileştirme seçildi; başka biri güçlendirmeyi alınca bot savaş davranışına döndü.
- Güçlendirmelerin sabit dünya yüksekliğinde doğması düzeltildi. Orman, çöl ve ay haritalarının toplam 12 doğma noktası gezinme ağı üzerinde erişilebilir bulundu.
- Seri atış etkisi botun karar verdiği atış bekleme süresini de kısaltıyor; iyileştirme mevcut bot can sınırını aşmıyor.
- Garaja sekiz ücretsiz tank rengi eklendi; her oyuncunun seçimi ayrı kaydediliyor.
- Beş tankın sekiz renkle toplam 40 önizleme birleşimi kontrol edildi; renk değiştirirken model yeniden oluşturulmuyor ve altın azalmıyor.
- 1920 × 1080 garaj görünümünde yazı taşması bulunmadı; ekran görüntüsü güncellendi.
- Tek oyunculu maçta turuncu gövde ve farklı renkte bot; iki oyunculu maçta turuncu/mor gövdeler ve HUD renkleri doğrulandı.
- Play yeniden açıldığında renk seçimleri korundu; raunt sıfırlamasında tank renkleri değişmedi.
- Bot canı kolayda 35, ortada 45, zorda 50 olarak sınırlandı; raunt sıfırlamalarında aynı değerler korundu.
- Gerçek iki oyunculu modda ağır tankın 75 canı korunuyor.
- Ana menü ekran görüntüsü mevcut dosyanın üzerine güncellendi.
- Süre sayacının yazı alanı ve boyutu taşmayı önleyecek şekilde düzeltildi.
- Sahneler, tanklar, haritalar ve temel oyun kaynakları GitHub yüklemesine dahil edildi.

## Menü ve altın

- Altın göstergesi ana menüde maç kurulum kartının, garajda gezinme çubuğunun içine yerleştirildi.
- 1920 × 1080 oyun görünümünde ana menü ve garaj görüntü üzerinden kontrol edildi.
- 2.147.483.647 altın metni kesilmeden sığdı; bakiye değiştirilmeksizin metin alanı sınandı.

## Kamera

- Tek oyunculu modda oyuncu merkezde takip edilir; uzak rakip yakınlaştırmayı bozmaz, yönü ekran kenarında gösterilir.
- İki oyunculu modda iki aktif tankın da görünmesi için kadraj ayarlanır.
- Üç farklı yükseklikte, 16:9, 16:10 ve 4:3 oranlarında toplam 3.240 hareketli konum kontrolü yapıldı; takip edilmesi gereken tanklar güvenli kadrajın dışına çıkmadı.
- Duraklatma sonrası hareket ve yakınlaştırma kontrol edildi; geçersiz kamera değerleri oluşmadı.
- Orman arenasında maç içi görünüm ve takip ayrıca kontrol edildi.

## Hasar

- Gerçek tank gövdesi ve mermi patlaması üzerinden ön, yan ve arka çarpışmalar denendi.
- Ön ×0,75; yan ×1,00; arka ×1,50 hasar çarpanları doğrulandı.
- Aynı patlamayı ikinci kez tetiklemek ilave hasar vermedi.
- Patlama mesafesi ve mevcut kalkan sistemi korunur; yön etkisi yalnızca doğrudan çarpılan tanka uygulanır.

## Derleme

- Windows 64 bit derlemesi başarılı: 0 hata, yaklaşık 178 MB.
- Oluşturulan Windows paketi açılış kontrolünü istisna hatası olmadan geçti.
- Tam Unity proje arşivi ayrı bir klasöre açıldı. Unity gerekli paketleri indirdi, varlıkları içe aktardı ve hem oyun hem editör kodlarını 0 derleme hatasıyla derledi; işlem 0 çıkış koduyla tamamlandı.
- Unity'nin eski proje ayarı biçimleri ve geliştirme paketleri için uyarıları hâlâ mevcut.
- Bu kontroller belirli bir okul bilgisayarının performansını veya uygulama çalıştırma izinlerini ölçmez.
