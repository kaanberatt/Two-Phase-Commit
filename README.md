# Two-Phase Commit (2PC)

Bu proje, **two-phase commit** işlemini temel alarak dağıtık sistemlerde transaction yönetimini örnekler. Sipariş, stok güncellemesi ve ödeme işlemlerini yöneten `Order.API`, `Stock.API` ve `Payment.API` mikroservisleri ile bir transaction yönetimi gerçekleştirilir. 

## İşlem Akışı

### 1. Başlangıç:
- **Kullanıcı:** Bir sipariş oluşturmak için sistemden `PUT Order` isteği yapar.
- **Coordinator :** Kullanıcının isteğini alır ve tüm süreci yönetmek için global bir transaction oluşturur.

### 2. Prepare Phase :
- **Coordinator:** İşlemin başlatıldığını ve hazırlık aşamasına geçildiğini belirtmek için `Order.API`, `Stock.API` ve `Payment.API` mikroservislerine "hazır ol" komutu gönderir.
- **Order.API:** Coordinator'dan gelen "hazır ol" komutunu alır, hazırlık işlemlerini tamamlar ve hazır olduğuna dair bir yanıt geri gönderir.
- **Stock.API:** Gerekli hazırlıkları tamamlar ve hazır olduğunu bildirir.
- **Payment.API:** Ödeme ile ilgili hazırlıkları gerçekleştirir ve hazır olduğunu bildirir.

### 3. Pre-Commit Phase :
- **Coordinator:** Tüm mikroservislerden olumlu yanıtları aldıktan sonra, tüm servislerden değişikliklerin kalıcı hale getirilmesi için `commit` komutu gönderir.

### 4. Commit Phase :
- **Order.API**, **Stock.API**, **Payment.API:** Aldıkları `commit` komutunu uygularlar. Bu komut, işlemleri kalıcı hale getirir. Her bir servis, işlem başarıyla tamamlandığında başarıyla tamamlandığına dair bir onay mesajı Coordinator'a gönderir.

### 5. Rollback Durumu:
- **Herhangi bir Hata Durumu:** Eğer `prepare` veya `commit` aşamasında herhangi bir servisten hata yanıtı alınırsa, Coordinator tüm işlemleri iptal eder ve rollback komutunu gönderir.
- **Rollback İşlemi:** Coordinator, rollback işlemini başlatır. Buna göre, `Order.API`, `Stock.API` ve `Payment.API` mikroservisleri yapılan değişiklikleri geri alır ve sistem önceki durumuna döner.

## Transaction Yönetimi

Transaction yönetimi, işlemlerin bütünlüğünü sağlamak ve tutarlılığı korumak için iki aşamalı commit protokolü kullanılarak gerçekleştirilir. İşlem başarılı bir şekilde tamamlanana kadar sistemde hiçbir değişiklik kalıcı olmaz. Eğer bir işlem başarısız olursa tüm servislerde yapılan değişiklikler geri alınır.

## Avantajlar:
Dağıtık sistemlerde işlemler birbirinden bağımsız çalışan servislerde gerçekleşse bile, iki aşamalı commit protokolü sayesinde tüm servislerin işlemleri başarıyla tamamlanmadığı sürece veri tutarlılığı korunur.

Eğer herhangi bir servis işlemi gerçekleştiremezse veya hata oluşursa, tüm yapılan işlemler geri alınır, bu sayede veri tutarlılığı bozulmaz.

İşlemler atomicity (bütünlük), consistency (tutarlılık), isolation (izolasyon) ve durability (kalıcılık) ilkelerine göre yapılır (ACID), bu da sistemi daha güvenilir hale getirir.

## Dezavantajlar:
İki aşamalı commit işlemi her aşamada yanıt beklemek zorunda olduğu için gecikmelere sebep olabilir ve performansı olumsuz etkileyebilir. Her adımın tamamlanması beklenir, bu da işlem süresini artırabilir.

Coordinator, tüm işlem akışını yönettiği için eğer bir hata meydana gelirse veya coordinator arızalanırsa işlem başarısız olabilir ve sistemin güvenilirliği zayıflayabilir.

Transaction işlemi boyunca kaynaklar kilitlenmiş durumda kalır. Eğer transaction tamamlanamazsa, kilitlenmiş kaynaklar sistemin diğer işlemleri üzerinde olumsuz etki yaratabilir.

Dağıtık sistemlerde ağ gecikmeleri, bağlantı sorunları gibi problemler olabileceği için, bu tür sistemleri yönetmek ve iki aşamalı commit işlemlerini her zaman sorunsuz yürütmek zor olabilir.
