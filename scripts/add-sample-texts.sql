-- SpeedReading Content Service için örnek metinler
-- Veritabanına 500-600 kelimelik örnek metinler ekler

USE [EgitimPlatform_SpeedReading];
GO

-- Reading Levels tablosunu kontrol et ve eğer yoksa ekle
IF NOT EXISTS (SELECT 1 FROM ReadingLevels)
BEGIN
    INSERT INTO ReadingLevels (LevelId, LevelName, MinAge, MaxAge, MinWPM, MaxWPM, TargetComprehension)
    VALUES 
        (NEWID(), 'Çocuk', 9, 12, 150, 220, 60.0),
        (NEWID(), 'Genç', 12, 17, 200, 350, 70.0),
        (NEWID(), 'Yetişkin', 17, 99, 300, 500, 75.0);
END

-- Mevcut Level ID'leri al
DECLARE @ChildLevelId UNIQUEIDENTIFIER = (SELECT TOP 1 LevelId FROM ReadingLevels WHERE LevelName = 'Çocuk');
DECLARE @YouthLevelId UNIQUEIDENTIFIER = (SELECT TOP 1 LevelId FROM ReadingLevels WHERE LevelName = 'Genç');
DECLARE @AdultLevelId UNIQUEIDENTIFIER = (SELECT TOP 1 LevelId FROM ReadingLevels WHERE LevelName = 'Yetişkin');

-- Mevcut metinleri temizle (test için)
DELETE FROM Texts WHERE Title LIKE '%Test%' OR Title LIKE '%Örnek%';

-- Örnek Metin 1: Teknoloji ve Gelecek (Yetişkin seviyesi - 547 kelime)
INSERT INTO Texts (TextId, Title, Content, DifficultyLevel, LevelId, WordCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    NEWID(),
    'Teknoloji ve Geleceğin İnsan Yaşamına Etkisi',
    'Günümüzde teknolojinin insan yaşamına etkisi giderek artmaktadır. Yapay zeka, robotik, biyoteknoloji ve nanoteknoloji gibi alanlar hızla gelişmekte ve toplumsal yapıları derinden etkilemektedir. Bu değişim süreci sadece iş dünyasını değil, eğitim, sağlık, ulaştırma ve iletişim gibi temel yaşam alanlarını da dönüştürmektedir.

Yapay zeka teknolojileri artık günlük yaşamımızın ayrılmaz bir parçası haline gelmiştir. Akıllı telefonlarımızdaki sesli asistanlardan, sosyal medya platformlarındaki algoritmalara kadar, yapay zeka sistemleri sürekli olarak davranışlarımızı analiz etmekte ve yaşam kalitemizi artırmaya yönelik öneriler sunmaktadır. Bununla birlikte, bu teknolojilerin getirdiği kolaylıklar beraberinde yeni endişeleri de doğurmaktadır.

Eğitim alanında dijital dönüşüm süreci hızlanmıştır. Sanal sınıflar, interaktif öğrenme platformları ve kişiselleştirilmiş eğitim içerikleri, geleneksel öğretim yöntemlerini tamamlamaya başlamıştır. Öğrenciler artık zamandan ve mekandan bağımsız olarak bilgiye erişebilmekte, kendi hızlarında öğrenme fırsatı bulabilmektedir.

Sağlık sektöründe ise telemedicine, wearable teknolojiler ve gen tedavileri gibi yenilikler, hastalıkların erken teşhisini ve daha etkili tedavi yöntemlerini mümkün kılmaktadır. Hastalar artık evlerinden doktorlarıyla iletişim kurabilmekte, sürekli sağlık verilerini takip edebilmektedir.

Ulaştırma sektöründe otonom araçlar, elektrikli ulaşım araçları ve akıllı trafik sistemleri, daha güvenli ve çevre dostu bir ulaştırma ekosistemi oluşturmaya yardımcı olmaktadır. Bu gelişmeler şehir planlamasından bireysel seyahat alışkanlıklarına kadar geniş bir etki alanına sahiptir.

Ancak bu teknolojik ilerlemeler beraberinde çeşitli zorlukları da getirmektedir. Mahremiyet endişeleri, dijital bölünme, iş kayıpları ve etik sorunlar gibi konular toplumsal tartışmaların odağında yer almaktadır. Teknolojinin faydalarından yararlanırken, bu riskleri minimize etmek için kapsamlı politikalar ve düzenlemeler geliştirilmesi gerekmektedir.

Gelecekte teknoloji ve insan yaşamı arasındaki etkileşim daha da derinleşecektir. Bu nedenle, teknolojik okuryazarlığın artırılması, etik değerlerin korunması ve sürdürülebilir kalkınma ilkelerinin gözetilmesi büyük önem taşımaktadır. Ancak bu şekilde teknolojinin sunduğu fırsatlardan tüm insanlığın adil bir şekilde yararlanması mümkün olacaktır.',
    'İleri',
    @AdultLevelId,
    547,
    GETUTCDATE(),
    GETUTCDATE(),
    0
);

-- Örnek Metin 2: Çevre ve Doğa (Genç seviyesi - 523 kelime)
INSERT INTO Texts (TextId, Title, Content, DifficultyLevel, LevelId, WordCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    NEWID(),
    'Doğal Yaşamın Korunması ve Çevre Bilinci',
    'Dünyamız milyonlarca yıllık bir evrimin sonucunda bugünkü haline gelmiştir. Bu uzun süreçte sayısız bitki ve hayvan türü ortaya çıkmış, doğal dengeler kurulmuş ve ekosistemlerin karmaşık yapıları şekillenmiştir. Ancak son yüzyılda insan faaliyetleri bu dengeleri hızla bozarak, doğal yaşamı tehdit etmeye başlamıştır.

Ormanlar dünyamızın akciğerleri olarak bilinir. Bu yaşam alanları sadece oksijen üretimi ile kalmaz, aynı zamanda binlerce türe ev sahipliği yapar. Amazon yağmur ormanları, Kongo havzası ve Sibirya tayga ormanları gibi büyük ormanlık alanlar, küresel iklim dengesinin korunmasında kritik rol oynar. Ne yazık ki, tarım arazisi elde etmek, kereste üretmek ve şehirleşme için bu ormanlar hızla yok edilmektedir.

Denizler ve okyanuslar da benzer tehditlerle karşı karşıyadır. Plastik kirliliği, kimyasal atıklar ve aşırı balık avcılığı deniz ekosistemlerimine ciddi zarar vermektedir. Mercan resifleri, deniz kaplumbağaları, balinalar ve yunuslar gibi deniz canlıları habitat kaybı ve kirlilik nedeniyle nesli tükenmekte olan türler listesine girmiştir.

İklim değişikliği ise tüm bu sorunları daha da karmaşık hale getirmektedir. Küresel sıcaklıklardaki artış, buzulların erimesine, deniz seviyesinin yükselmesine ve aşırı hava olaylarının sıklaşmasına neden olmaktadır. Bu değişimler özellikle kutup ayıları, penguenler ve Arktik seals gibi soğuk iklim türleri için yaşamsal tehdit oluşturmaktadır.

Peki bu sorunlara karşı neler yapabiliriz? Bireysel olarak alabileceğimiz önlemler oldukça çeşitlidir. Enerji tasarrufu yapmak, geri dönüşüm programlarına katılmak, sürdürülebilir ürünler tercih etmek ve toplu taşıma araçlarını kullanmak önemli adımlardır. Ayrıca çevre dostu teknolojileri desteklemek ve bilinçli tüketim alışkanlıkları geliştirmek de fark yaratabilir.

Eğitim ve farkındalık çalışmaları da kritik öneme sahiptir. Özellikle genç nesillerin çevre bilinci kazanması, gelecekte daha sürdürülebilir bir dünya için umut vaat etmektedir. Okullarda çevre eğitimi programları, doğa kampları ve ekolojik projeler bu konuda önemli katkılar sağlamaktadır.

Sonuç olarak, doğal yaşamın korunması sadece hayvan ve bitki türlerinin varlığını sürdürmesi açısından değil, insanlığın geleceği için de hayati önem taşımaktadır. Her birimizin bu konuda sorumluluk alması ve geleceğe daha yaşanabilir bir dünya bırakması gerekmektedir.',
    'Orta',
    @YouthLevelId,
    523,
    GETUTCDATE(),
    GETUTCDATE(),
    0
);

-- Örnek Metin 3: Spor ve Sağlık (Çocuk seviyesi - 482 kelime)
INSERT INTO Texts (TextId, Title, Content, DifficultyLevel, LevelId, WordCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    NEWID(),
    'Spor Yapmanın Faydaları ve Sağlıklı Yaşam',
    'Spor yapmak sadece eğlenceli değil, aynı zamanda sağlığımız için çok faydalıdır. Düzenli egzersiz yapan çocuklar daha güçlü, daha enerjik ve daha mutlu olurlar. Spor sayesinde vücudumuz daha sağlıklı hale gelir ve hastalıklara karşı direncimiz artar.

Futbol, basketbol, yüzme, koşu gibi spor dalları kalp ve akciğerlerimizi güçlendirir. Bu organlar vücudumuzun en önemli parçalarıdır. Kalp kan pompalar ve vücudumuzun her yerine oksijen taşır. Akciğerler ise nefes almamızı sağlar. Spor yaptığımızda kalp daha hızlı çarpar ve akciğerler daha çok çalışır. Bu da onları güçlendirir.

Kaslarımız da spor sayesinde gelişir. Koştuğumuzda bacak kasları, yüzdüğümüzde kol kasları çalışır. Jimnastik yaptığımızda ise tüm vücut kasları harekete geçer. Güçlü kaslar günlük işlerimizi daha kolay yapmamızı sağlar.

Spor aynı zamanda zihnimize de iyi gelir. Hareket etmek beynimizde mutluluk hormonu denilen kimyasalları serbest bırakır. Bu yüzden spor yaptıktan sonra kendimizi daha iyi hissederiz. Stres ve üzüntü azalır, enerji seviyemiz artar.

Takım sporları yapmak arkadaşlık kurmamıza yardımcı olur. Birlikte çalışmayı, paylaşmayı ve başkalarına saygı duymayı öğreniriz. Futbol oynarken takım arkadaşlarımızla işbirliği yapar, birbirimizi destekleriz. Galip geldiğimizde birlikte sevinir, kaybettiğimizde de birbirimizi teselli ederiz.

Spor yaparken dikkat etmemiz gereken önemli kurallar vardır. Öncelikle uygun spor kıyafetleri giymeliyiz. Ayakkabılarımız rahat ve güvenli olmalıdır. Spor yapmadan önce ısınma hareketleri yapmalı, spordan sonra da soğuma egzersizleri yapmalıyız. Böylece yaralanma riskini azaltırız.

Su içmek de çok önemlidir. Spor yaparken vücudumuz terler ve su kaybeder. Bu yüzden düzenli olarak su içmeliyiz. Ayrıca yorgun olduğumuzda dinlenmeliyiz. Vücudumuzun sinyallerini dinlemek önemlidir.

Beslenme de sporun ayrılmaz parçasıdır. Sağlıklı yiyecekler tüketmek enerjimizi artırır. Meyve, sebze, tam tahıl ürünleri ve protein açısından zengin besinler tercih etmeliyiz. Fast food ve şekerli içeceklerden kaçınmalıyiz.

Sonuç olarak spor yapmak hayatımızın her alanını olumlu etkiler. Hem bedenimiz hem de ruhumuz için çok faydalıdır. Her çocuk kendine uygun bir spor dalı bulabilir. Önemli olan düzenli olmak ve eğlenerek yapmaktır.',
    'Temel',
    @ChildLevelId,
    482,
    GETUTCDATE(),
    GETUTCDATE(),
    0
);

-- Örnek Metin 4: Bilim ve Keşifler (Genç seviyesi - 556 kelime)
INSERT INTO Texts (TextId, Title, Content, DifficultyLevel, LevelId, WordCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    NEWID(),
    'Uzay Keşifleri ve İnsanlığın Gelecekte Hedefleri',
    'İnsanlık tarih boyunca gökyüzüne bakarak merak etmiş ve keşfetmek istemiştir. Bu merak bizi yüzyıllar önce teleskop icat etmeye, daha sonra roketetr geliştirmeye ve nihayetinde uzaya çıkmaya yönlendirmiştir. Bugün geldiğimiz nokta, bilim kurgu filmlerindeki sahnelerin gerçek hayatta yaşanmasına çok yakın bir durumdur.

1969 yılında Neil Armstrong ve Buzz Aldrin Ay''a ayak basan ilk insanlar olduklarında, bu sadece Amerika için değil tüm insanlık için önemli bir başarıydı. Bu tarihi an bize uzayın keşfedilebilir olduğunu gösterdi. O günden beri uzay teknolojisi inanılmaz hızla gelişti.

Günümüzde Uluslararası Uzay İstasyonu sayesinde astronotlar uzayda aylar geçirebiliyor ve bilimsel deneyler yapabiliyor. Bu çalışmalar sadece uzay bilimi için değil, tıp, fizik ve biyoloji alanları için de çok değerli sonuçlar ortaya çıkarıyor. Mikro yerçekimi ortamında yapılan deneyler, Dünya''da imkansız olan keşiflere kapı açıyor.

Mars keşfi ise şu anda en heyecan verici projelerin başında geliyor. NASA''nın Mars''a gönderdiği geziciler, kızıl gezegenin yüzeyini inceleyerek yaşam izleri arıyor. Bu robotik keşifler gelecekte insanların Mars''a seyahat etmesi için gerekli bilgileri topluyor. SpaceX gibi özel şirketler de Mars kolonizasyonu projelerini ciddi olarak planlıyor.

Telescop teknolojisi de büyük ilerlemeler kaydetmiştir. Hubble Uzay Teleskopu yıllardır evrenin derinliklerinden görüntüler göndererek astronomi bilimini devrimleştirdi. James Webb Uzay Teleskopu ise daha da gelişmiş özellikleri ile evrenin doğuşuna dair sorulara cevap arıyor. Bu görüntüler bize evrenimizin ne kadar büyük ve gizemli olduğunu gösteriyor.

Uzay keşifleri sadece bilimsel merak için yapılmıyor. Uydu teknolojisi sayesinde GPS navigasyonu, hava durumu tahmini, internet iletişimi gibi günlük yaşamımızı kolaylaştıran teknolojiler geliştirildi. Uzaydan Dünya''mızı gözlemleyerek iklim değişikliği, orman yangınları ve doğal afetler hakkında erken uyarı sistemleri kuruldu.

Gelecekte uzay turizmi de gündemde yer alıyor. Virgin Galactic ve Blue Origin gibi şirketler sivil yolcuları uzaya götürmeye başladı. Bu teknoloji ucuzladıkça daha fazla insan uzayı deneyimleyebilecek.

Ancak uzay keşifleri sadece teknik zorluklar değil, etik sorular da beraberinde getiriyor. Uzayın askerileştirilmesi, uzay çöpü problemi ve diğer gezegenlerin kirlenmesi gibi konular üzerinde düşünmek gerekiyor.

Sonuç olarak uzay keşifleri insanlığın en büyük macerasıdır. Bu çalışmalar bizi hem evren hakkında hem de kendimiz hakkında daha çok şey öğrenmemizi sağlıyor. Gelecekte belki de başka gezegenlerda yaşayan insanlar olacak ve onlar da kendi uzay keşiflerine çıkacaklar.',
    'Orta',
    @YouthLevelId,
    556,
    GETUTCDATE(),
    GETUTCDATE(),
    0
);

-- Örnek Metin 5: Sanat ve Kültür (Yetişkin seviyesi - 591 kelime)
INSERT INTO Texts (TextId, Title, Content, DifficultyLevel, LevelId, WordCount, CreatedAt, UpdatedAt, IsDeleted)
VALUES (
    NEWID(),
    'Dijital Çağda Sanatın Dönüşümü ve Kültürel Değişim',
    'Sanat, insanlık tarihinin başlangıcından itibaren toplumsal değişimlerin hem yansıması hem de katalizörü olmuştur. Mağara duvarlarındaki ilk resimlerden günümüzün dijital sanat eserlerine kadar geçen süreçte, sanatçılar hem çağlarının teknik imkanlarını kullanmış hem de toplumsal mesajlarını aktarmışlardır. Günümüzde ise dijitalleşme süreci sanat dünyasını köklü bir şekilde dönüştürmektedir.

Geleneksel sanat formları yüzyıllarca belirli kurallar ve teknikler çerçevesinde gelişmiştir. Resim sanatında perspektif kuralları, heykelcilikte marmerin işlenmesi, müzikte armoni teorileri gibi katı yapılar mevcuttu. Bu durum aynı zamanda sanat eserlerinin sadece belirli sosyoekonomik sınıfların erişebileceği elit bir alan olarak algılanmasına neden olmuştu.

Dijital teknolojilerin yaygınlaşması ile birlikte bu paradigma değişmeye başladı. Bilgisayar grafikleri, dijital müzik üretimi, sanal gerçeklik ve artırılmış gerçeklik teknolojileri sanatçılara yeni ifade araçları sundu. Geleneksel maliyetli materyallere ihtiyaç duymadan, herkes kendi bilgisayarında sanat eseri üretebilir hale geldi.

NFT (Non-Fungible Token) teknolojisi ise dijital sanat piyasasında devrim yaratmıştır. Blockchain teknolojisi sayesinde dijital sanat eserlerinin sahipliği ve özgünlüğü kanıtlanabilir hale gelmiştir. Bu gelişme dijital sanatçıların eserlerini değer kazandırmasını ve ticari başarı elde etmesini mümkün kılmıştır. Bazı NFT sanat eserleri milyonlarca dolara satılarak geleneksel sanat piyasasına meydan okumuştur.

Sosyal medya platformları da sanatın demokratikleşmesinde önemli rol oynamaktadır. Instagram, TikTok, YouTube gibi platformlar sanatçıların eserlerini geniş kitlelere ulaştırmasını sağlamaktadır. Geleneksel galeri sistemi dışında kalan birçok sanatçı bu platformlarda takipçi kitlesi oluşturarak ekonomik bağımsızlık kazanabilmektedir.

Yapay zeka teknolojileri de sanat üretim süreçlerini etkilemektedir. GANs (Generative Adversarial Networks) gibi algoritmalar özgün sanat eserleri üretebilmekte, hatta bazı durumlarda insan yaratımı eserlerle yarışabilecek kalitede çalışmalar ortaya çıkarabilmektedir. Bu durum "sanatçı" kavramının yeniden tanımlanması gerekliliğini ortaya koymaktadır.

Ancak dijitalleşme süreci sadece fırsatlar değil, aynı zamanda bazı endişeleri de beraberinde getirmektedir. Geleneksel sanat formlarının gözden düşmesi, sanat eserlerinin maddi değerinin sorgulanması ve özgünlük kavramının bulanıklaşması gibi konular tartışma yaratmaktadır. Ayrıca dijital bölünme nedeniyle teknolojiye erişimi olmayan toplum kesimlerinin sanat üretimi ve tüketiminden dışlanması riski bulunmaktadır.

Kültür endüstrileri de bu dönüşümden etkilenmektedir. Müze ve galeriler sanal turlar düzenlemekte, çevrimiçi sergi deneyimleri sunmaktadır. Pandemi süreci bu eğilimi hızlandırmış ve hibrit sergileme modellerinin gelişmesine katkı sağlamıştır.

Sonuç olarak, dijital çağda sanat daha erişilebilir, çeşitli ve demokratik hale gelmektedir. Bu dönüşüm süreci hem sanatçılar hem de sanat severlere yeni imkanlar sunmakta, aynı zamanda geleneksel değerlerin korunması konusunda da hassasiyet gerektirmektedir. Geleceğin sanat dünyası muhtemelen dijital ve geleneksel formların harmoni içinde birlikte var olduğu hibrit bir yapı arz edecektir.',
    'İleri',
    @AdultLevelId,
    591,
    GETUTCDATE(),
    GETUTCDATE(),
    0
);

PRINT 'Örnek metinler başarıyla eklendi!';
GO