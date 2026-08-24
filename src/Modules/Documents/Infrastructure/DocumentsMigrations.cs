using Npgsql;

namespace Workforce.Modules.Documents.Infrastructure;

public static class DocumentsMigrations
{
    public static async Task ApplyMigrationsAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct);

        const string sql = @"
            CREATE SCHEMA IF NOT EXISTS documents;

            CREATE TABLE IF NOT EXISTS documents.document_types (
                id UUID PRIMARY KEY,
                code VARCHAR(50) NOT NULL UNIQUE,
                name_en VARCHAR(200) NOT NULL,
                name_ar VARCHAR(200) NOT NULL,
                is_required BOOLEAN NOT NULL DEFAULT FALSE,
                requires_expiry_date BOOLEAN NOT NULL DEFAULT FALSE,
                allowed_mime_types VARCHAR(255) NOT NULL DEFAULT 'application/pdf,image/png,image/jpeg',
                max_size_bytes BIGINT NOT NULL DEFAULT 10485760
            );

            CREATE TABLE IF NOT EXISTS documents.documents (
                id UUID PRIMARY KEY,
                tenant_id UUID NOT NULL,
                legal_entity_id UUID NOT NULL,
                owner_type VARCHAR(50) NOT NULL,
                owner_id UUID NOT NULL,
                document_type_id UUID NOT NULL REFERENCES documents.document_types(id),
                title VARCHAR(200) NOT NULL,
                status INT NOT NULL DEFAULT 1, -- Active
                expiry_date DATE NULL,
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                created_by UUID NOT NULL
            );

            CREATE TABLE IF NOT EXISTS documents.document_versions (
                id UUID PRIMARY KEY,
                document_id UUID NOT NULL REFERENCES documents.documents(id),
                version_number INT NOT NULL DEFAULT 1,
                storage_key VARCHAR(500) NOT NULL,
                file_name VARCHAR(255) NOT NULL,
                file_size BIGINT NOT NULL,
                content_type VARCHAR(100) NOT NULL,
                sha256_checksum VARCHAR(64) NOT NULL DEFAULT '',
                uploaded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                uploaded_by UUID NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_documents_owner ON documents.documents(tenant_id, owner_type, owner_id);
            CREATE INDEX IF NOT EXISTS ix_doc_versions_doc ON documents.document_versions(document_id, version_number DESC);

            -- Seed standard Document Types if table is empty
            INSERT INTO documents.document_types (id, code, name_en, name_ar, is_required, requires_expiry_date)
            VALUES 
                ('a1111111-1111-1111-1111-111111111111', 'NATIONAL_ID', 'National ID / Iqama', 'الهوية الوطنية / الإقامة', true, true),
                ('a2222222-2222-2222-2222-222222222222', 'PASSPORT', 'Passport', 'جواز السفر', false, true),
                ('a3333333-3333-3333-3333-333333333333', 'CONTRACT', 'Employment Contract', 'عقد العمل', true, false),
                ('a4444444-4444-4444-4444-444444444444', 'DEGREE', 'Degree / Diploma Certificate', 'المؤهل العلمي / الشهادة', false, false),
                ('a5555555-5555-5555-5555-555555555555', 'OTHER', 'General Attachment', 'مرفق عام', false, false),
                ('a6666666-6666-6666-6666-666666666666', 'RESUME', 'Candidate Resume / CV', 'السيرة الذاتية للمرشح', false, false)
            ON CONFLICT (code) DO NOTHING;
        ";

        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
