### Speed Reading - Backlog ve Issue Listesi

#### Faz 1: Profil ve İçerik Yönetimi (Admin + API)
- [ ] ProfileService: Proje oluşturma, Swagger, temel health endpoint
- [ ] ProfileService: `GET/PUT /api/v1/profile/me` DTO ve controller
- [ ] ProfileService (Admin): `GET /api/v1/admin/profiles`, `GET/PUT /api/v1/admin/profiles/{userId}`, `GET /api/v1/admin/profiles/{userId}/history`
- [ ] ContentService: Proje oluşturma, Swagger, temel health endpoint
- [ ] ContentService (Admin): Texts CRUD (`/api/v1/admin/texts`) + import/export taslağı
- [ ] ContentService (Admin): Exercises CRUD (`/api/v1/admin/exercises`) + ExerciseTypes (`/api/v1/admin/exercise-types`)
- [ ] ContentService (Admin): Questions CRUD (`/api/v1/admin/questions`) + bulk ekleme
- [ ] ContentService (Admin): Levels CRUD (`/api/v1/admin/levels`)
- [ ] Admin Panel: SR menüsü ve rotalar (texts/exercises/questions/levels/profiles/reports)
- [ ] Admin Panel: Texts list/form iskeleti
- [ ] Admin Panel: Questions list/form iskeleti
- [ ] Admin Panel: Levels list/form iskeleti

#### Faz 2: İlerleme/Skor Kayıtları ve Raporlama
- [ ] ProgressService: Proje oluşturma, Swagger, health endpoint
- [ ] ProgressService (Client): `POST /api/v1/session/start|end`, `POST /api/v1/exercise/complete` idempotency
- [ ] ProgressService (Admin): `GET /api/v1/admin/sessions`, `GET /api/v1/admin/attempts`, `GET /api/v1/admin/responses`
- [ ] ProgressService (Admin): `GET /api/v1/admin/metrics/overview`, export uçları
- [ ] Admin Panel: Reports ekranı (filtreli tablolar + overview özetleri)

#### Faz 3: Olay Akışı ve ML Hazırlığı
- [ ] Event yayınları: `SessionCompleted`, `ExerciseCompleted`, `QuestionAnswered`
- [ ] ProgressService: `FeatureSnapshots` modeli ve uçları
- [ ] Günlük batch export (parquet/CSV) taslağı ve konfigürasyonu

#### Güvenlik/Yetki ve Gateway
- [ ] Identity: `sr.profile.manage`, `sr.content.manage`, `sr.progress.read.all`, `sr.progress.export` izinleri
- [ ] Admin panel `AUTHORIZATION_POLICIES` güncellemesi (sr rotaları)
- [ ] API Gateway: `/sr-profile/*`, `/sr-content/*`, `/sr-progress/*` rotaları

#### Test ve Dokümantasyon
- [ ] OpenAPI şemaları ve DTO senkronizasyonu
- [ ] Pact sözleşme testleri (admin-panel ↔ servisler)
- [ ] k6 yük senaryoları (session/end, exercise/complete, admin export)

