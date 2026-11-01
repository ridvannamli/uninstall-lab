# Otonom Mod (zamanlanmış turlar)

Yalnızca zamanlanmış görev veya kullanıcı "otonom" dediğinde geçerli. Diğer kurallara ek, onları gevşetmez.

## Onay ve soru
- Kullanıcıya soru sorma, onay bekleme. Zamanlanmış görevi başlatmak, `PLAN.md`'deki sıradaki aşamanın istendiği anlamına gelir.
- Kısa planı `rules/07_DURUM.md`'ye yaz ve uygula.
- Belirsizlikte en makul seçeneği seç, `DECISIONS.md`'ye tek satır (karar + gerekçe) yaz.
- Aynı hata için en fazla 2 kanıtlı düzeltme denemesi (rules/03). Çözülmezse engeli `07_DURUM.md > Bilinen sorunlar`'a yaz, sormadan sıradaki bağımsız aşamaya geç.

## Tur sınırı
- Tur başına tek aşama (`PLAN.md`'de ilk `[ ]`). Bitince `[x]` yap, `07_DURUM.md`'yi güncelle, commit + push, dur.
- `PLAN.md`'deki tüm aşamalar `[x]` ise hiçbir şey yapma; zamanlanmış görevi `update_trigger enabled=false` ile kapat.
- Dal: `dev`. Küçük commit'ler, her turda push. `main`'e dokunma.

## Yasaklar (güvenlik)
- Veri kaybı, geri dönüşü zor işlem veya dış sistem değişikliği yapma; atla ve not düş.
- Gerçek registry/dosya silme kodu yalnızca `dry-run` varsayılanı ve yedek alma ile yazılır. Test ortamında gerçek silme çalıştırılmaz (CI'da sahte/mocklanmış sistem).
- Gizli bilgi commit etme. Bağımlılık eklemeden önce zorunlu mu kontrol et.
- IObit veya başka ürünün kodu, adı, logosu, arayüzü kopyalanmaz.

## Doğrulama ve kota
- Container'da .NET SDK yoksa: kodu dikkatle yaz, doğrulamayı CI'a bırak (`gh run watch` / `gh run view --log-failed` ile yalnızca hata satırlarını oku).
- Windows CI işi ücretsiz kotayı harcar: tur başına en fazla bir Windows çalışması tetikle (tek push).
- Gerçek bir komut/CI kanıtı yoksa "tamamlandı" deme; doğrulanamayanı raporda yaz.
- Rapor ve token kuralları: rules/01 ve rules/02.

## Sürekli mod (kullanıcı istedi)
- Kota/süre elverdiği sürece aşamalar arasında durma; her aşama: uygula, push, CI yeşil, PLAN/DURUM güncelle, sıradaki aşamaya geç.
- Kullanıcıya soru sorma; önemli noktalarda (aşama bitti, çözülemeyen engel, plan bitti) tek satır PushNotification gönder.
- Zamanlanmış tur, son commit 40 dk'dan yeniyse çalışmaz (çakışma önlemi).
