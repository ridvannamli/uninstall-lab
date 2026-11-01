# Codex Genel Çalışma Talimatları

Bu belge, diğer kural belgeleriyle birlikte aynı `rules` klasöründe tutulur. Codex her görevde bu dosyayı, varsa `07_DURUM.md` dosyasını ve görevle ilgili kural belgesini okur.

## Ana hedef

Mevcut projenin kapsamını koruyarak küçük, çalışan, doğrulanmış ve anlaşılır değişiklikler yapmak.

Öncelik sırası:

1. Çalışan küçük adım
2. Doğrulama ve test
3. Minimum değişiklik
4. Minimum token
5. Projenin mevcut mimarisi ve araçları

## Zorunlu çalışma kuralları

- Aynı anda yalnızca bir aşama veya sorun üzerinde çalış.
- Kullanıcı açıkça istemeden sonraki aşamaya geçme.
- Önce mevcut dosyaları, kuralları ve durumu incele; sonra değişiklik yap.
- Kullanıcı "uygula" veya "doğrudan uygula" demeden dosya değiştirme.
- Mevcut teknoloji, paket yöneticisi ve test altyapısını kullan; gereksiz bağımlılık, katman veya dosya ekleme.
- Çalışan kodu kapsam dışında yeniden yazma.
- Veri şeması, yapılandırma, migration veya dış sistem değişikliği gerekiyorsa projenin yerleşik yöntemini kullan.
- Kod değişikliğinden sonra doğrudan ilgili en küçük doğrulamayı çalıştır; mümkünse sonra uygun tüm testleri çalıştır.
- Hata varsa `03_HATA_DURUMUNDA.md` belgesini uygula.
- Gizli bilgi, erişim anahtarı veya kişisel veriyi koda, belgelere ya da yanıta yazma.
- Kullanıcı dili ve mevcut proje adlandırma kurallarını koru.
- Gerçek komut, test veya manuel kontrol kanıtı olmadan tamamlandı deme.

## Her görev başlangıcında

1. Varsa `07_DURUM.md` ve görevle ilgili kural belgesini oku.
2. İstenen işle ilgili dosyaları incele.
3. Değişiklik gerekliyse en fazla 5 maddelik kısa plan ver.
4. Onay yoksa inceleme/plan aşamasında dur.
5. Kullanıcı "doğrudan uygula" dediyse planı bekletmeden uygula.

## Her görev sonunda

Yalnızca şu biçimde raporla:

- Yapılan:
- Değişen dosyalar:
- Doğrulama:
- Manuel kontrol:
- Sonraki önerilen tek adım:

Gerçek bir değişiklik tamamlandıysa `07_DURUM.md` dosyasını kısa biçimde güncelle.
