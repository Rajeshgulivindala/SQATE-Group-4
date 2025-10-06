using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Services.Data
{
    public static class StaffRepository
    {
        public static event EventHandler StaffUpserted;

        private static readonly string connectionString =
            "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=HMSDatabase;Integrated Security=True;";

        private const string StaffsFullName = "[HMSDatabase].[dbo].[Staffs]";

        // ---------------- DTOs ----------------
        public sealed class NewStaff
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }       // nullable
            public string Phone { get; set; }       // nullable
            public int? DepartmentID { get; set; }     // nullable
            public int? UserID { get; set; }     // nullable
        }

        public sealed class DepartmentRow
        {
            public int DepartmentID { get; set; }
            public string DepartmentName { get; set; }
        }

        // ---------------- Public API ----------------

        public static async Task<int> AddStaffAsync(NewStaff s)
        {
            if (string.IsNullOrWhiteSpace(s.FirstName)) throw new ArgumentException("FirstName required");
            if (string.IsNullOrWhiteSpace(s.LastName)) throw new ArgumentException("LastName required");

            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();

                // Safety: ensure correct DB
                using (var cmdDb = new SqlCommand("SELECT DB_NAME()", cn))
                {
                    var dbName = (string)await cmdDb.ExecuteScalarAsync();
                    if (!string.Equals(dbName, "HMSDatabase", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Connected to unexpected database: " + dbName + ". Expected HMSDatabase.");
                }

                // Ensure Departments exists/seeded to satisfy FK
                await EnsureDepartmentsSchemaAndSeedAsync(cn);

                try
                {
                    return await InsertIntoStaffsAsync(cn, s);
                }
                catch (SqlException ex) when (
                    // 207: invalid column; 515: NULL into NOT NULL; 547: FK violation
                    ex.Errors.Cast<System.Data.SqlClient.SqlError>().Any(er => er.Number == 207 || er.Number == 515 || er.Number == 547)
                )
                {
                    await EnsureStaffsColumnsAsync(cn);
                    await EnsureDepartmentsSchemaAndSeedAsync(cn);
                    return await InsertIntoStaffsAsync(cn, s);
                }
            }
        }

        public static async Task<List<DepartmentRow>> GetDepartmentsAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                await EnsureDepartmentsSchemaAndSeedAsync(cn);

                var list = new List<DepartmentRow>();
                // Prefer DepartmentName if present; otherwise use Name
                var nameCol = (await ColumnExistsAsync(cn, "dbo", "Departments", "DepartmentName"))
                              ? "DepartmentName" : "Name";

                using (var cmd = new SqlCommand(
                    "SELECT DepartmentID, " + nameCol + " FROM dbo.Departments ORDER BY " + nameCol + ";", cn))
                using (var r = await cmd.ExecuteReaderAsync())
                {
                    while (await r.ReadAsync())
                    {
                        list.Add(new DepartmentRow
                        {
                            DepartmentID = r.GetInt32(0),
                            DepartmentName = r.GetString(1)
                        });
                    }
                }
                return list;
            }
        }

        // ---------------- Internal helpers ----------------

        private static async Task<int> InsertIntoStaffsAsync(SqlConnection cn, NewStaff s)
        {
            const string sql = @"
INSERT INTO " + StaffsFullName + @"
    (FirstName, LastName, Email, Phone, DepartmentID, IsActive, CreatedDate, UserID)
VALUES
    (@fn, @ln, @em, @ph, @dept, 1, @cd, @uid);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@fn", s.FirstName.Trim());
                cmd.Parameters.AddWithValue("@ln", s.LastName.Trim());
                cmd.Parameters.AddWithValue("@em", (object)s.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ph", (object)s.Phone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dept", (object)s.DepartmentID ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@cd", DateTime.UtcNow);
                cmd.Parameters.AddWithValue("@uid", (object)s.UserID ?? DBNull.Value);

                var idObj = await cmd.ExecuteScalarAsync();
                var newId = (idObj == null || idObj == DBNull.Value) ? 0 : Convert.ToInt32(idObj);

                if (StaffUpserted != null) StaffUpserted.Invoke(null, EventArgs.Empty);
                return newId;
            }
        }

        /// Ensures dbo.Staffs exists and has defaults for NOT NULL columns we don't populate.
        private static async Task EnsureStaffsColumnsAsync(SqlConnection cn)
        {
            const string patchSql = @"
IF OBJECT_ID('dbo.Staffs','U') IS NULL
BEGIN
    CREATE TABLE dbo.Staffs
    (
        StaffID     INT IDENTITY(1,1) PRIMARY KEY,
        FirstName   NVARCHAR(50) NOT NULL,
        LastName    NVARCHAR(50) NOT NULL
    );
END;

IF COL_LENGTH('dbo.Staffs','Email') IS NULL
    ALTER TABLE dbo.Staffs ADD Email NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.Staffs','Phone') IS NULL
    ALTER TABLE dbo.Staffs ADD Phone NVARCHAR(25) NULL;

IF COL_LENGTH('dbo.Staffs','DepartmentID') IS NULL
    ALTER TABLE dbo.Staffs ADD DepartmentID INT NULL;

IF COL_LENGTH('dbo.Staffs','IsActive') IS NULL
    ALTER TABLE dbo.Staffs ADD IsActive BIT NOT NULL CONSTRAINT DF_Staffs_IsActive DEFAULT(1);

IF COL_LENGTH('dbo.Staffs','CreatedDate') IS NULL
    ALTER TABLE dbo.Staffs ADD CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Staffs_CreatedDate DEFAULT SYSUTCDATETIME();

IF COL_LENGTH('dbo.Staffs','UserID') IS NULL
    ALTER TABLE dbo.Staffs ADD UserID INT NULL;

-- EmployeeCode with default
IF COL_LENGTH('dbo.Staffs','EmployeeCode') IS NULL
BEGIN
    ALTER TABLE dbo.Staffs 
        ADD EmployeeCode NVARCHAR(20) NOT NULL 
            CONSTRAINT DF_Staffs_EmployeeCode DEFAULT (CONCAT('EMP-', RIGHT(CONVERT(varchar(12), ABS(CHECKSUM(NEWID()))), 6)));
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Staffs') AND c.name = 'EmployeeCode'
)
BEGIN
    ALTER TABLE dbo.Staffs
        ADD CONSTRAINT DF_Staffs_EmployeeCode DEFAULT (CONCAT('EMP-', RIGHT(CONVERT(varchar(12), ABS(CHECKSUM(NEWID()))), 6))) FOR EmployeeCode;
END;

-- Position default
IF COL_LENGTH('dbo.Staffs','Position') IS NULL
BEGIN
    ALTER TABLE dbo.Staffs ADD Position NVARCHAR(50) NOT NULL CONSTRAINT DF_Staffs_Position DEFAULT ('Staff');
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Staffs') AND c.name = 'Position'
)
BEGIN
    ALTER TABLE dbo.Staffs ADD CONSTRAINT DF_Staffs_Position DEFAULT ('Staff') FOR Position;
END;

-- Address default
IF COL_LENGTH('dbo.Staffs','Address') IS NULL
    ALTER TABLE dbo.Staffs ADD Address NVARCHAR(255) NOT NULL CONSTRAINT DF_Staffs_Address DEFAULT ('');

-- HireDate default
IF COL_LENGTH('dbo.Staffs','HireDate') IS NULL
BEGIN
    ALTER TABLE dbo.Staffs ADD HireDate DATETIME2 NOT NULL CONSTRAINT DF_Staffs_HireDate DEFAULT SYSUTCDATETIME();
END
ELSE IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID('dbo.Staffs') AND c.name = 'HireDate'
)
BEGIN
    ALTER TABLE dbo.Staffs ADD CONSTRAINT DF_Staffs_HireDate DEFAULT SYSUTCDATETIME() FOR HireDate;
END;

-- Salary default
IF COL_LENGTH('dbo.Staffs','Salary') IS NULL
    ALTER TABLE dbo.Staffs ADD Salary DECIMAL(18,2) NOT NULL CONSTRAINT DF_Staffs_Salary DEFAULT (0);

-- Qualifications default
IF COL_LENGTH('dbo.Staffs','Qualifications') IS NULL
    ALTER TABLE dbo.Staffs ADD Qualifications NVARCHAR(255) NOT NULL CONSTRAINT DF_Staffs_Qualifications DEFAULT ('');

-- LicenseNumber default
IF COL_LENGTH('dbo.Staffs','LicenseNumber') IS NULL
    ALTER TABLE dbo.Staffs ADD LicenseNumber NVARCHAR(100) NOT NULL CONSTRAINT DF_Staffs_LicenseNumber DEFAULT ('');
";
            using (var cmd = new SqlCommand(patchSql, cn))
                await cmd.ExecuteNonQueryAsync();
        }

        /// Ensure Departments exists; ensure BOTH DepartmentName and Name columns exist; keep them in sync;
        /// seed with both columns to avoid NOT NULL errors on legacy 'Name'.
        private static async Task EnsureDepartmentsSchemaAndSeedAsync(SqlConnection cn)
        {
            // Create table if missing (with BOTH columns nullable to start)
            const string createSql = @"
IF OBJECT_ID('dbo.Departments','U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        DepartmentID   INT IDENTITY(1,1) PRIMARY KEY,
        DepartmentName NVARCHAR(100) NULL,
        Name           NVARCHAR(100) NULL
    );
END;";
            using (var cmd = new SqlCommand(createSql, cn))
                await cmd.ExecuteNonQueryAsync();

            // Ensure both columns exist (idempotent)
            if (!await ColumnExistsAsync(cn, "dbo", "Departments", "DepartmentName"))
                using (var cmd = new SqlCommand("ALTER TABLE dbo.Departments ADD DepartmentName NVARCHAR(100) NULL;", cn))
                    await cmd.ExecuteNonQueryAsync();

            if (!await ColumnExistsAsync(cn, "dbo", "Departments", "Name"))
                using (var cmd = new SqlCommand("ALTER TABLE dbo.Departments ADD Name NVARCHAR(100) NULL;", cn))
                    await cmd.ExecuteNonQueryAsync();

            // Sync values between the two name columns (whichever is present)
            using (var cmd = new SqlCommand(@"
-- Trim both
UPDATE dbo.Departments SET DepartmentName = LTRIM(RTRIM(DepartmentName));
UPDATE dbo.Departments SET Name           = LTRIM(RTRIM(Name));

-- Backfill DepartmentName from Name
UPDATE dbo.Departments SET DepartmentName = Name WHERE DepartmentName IS NULL AND Name IS NOT NULL;

-- Backfill Name from DepartmentName
UPDATE dbo.Departments SET Name = DepartmentName WHERE Name IS NULL AND DepartmentName IS NOT NULL;

-- Still-empty names -> fallback unique label
UPDATE d
SET DepartmentName = COALESCE(DepartmentName, N'Department ' + CONVERT(NVARCHAR(10), d.DepartmentID)),
    Name           = COALESCE(Name,           N'Department ' + CONVERT(NVARCHAR(10), d.DepartmentID))
FROM dbo.Departments d;

-- De-duplicate (case-insensitive) DepartmentName by suffixing
;WITH c AS
(
    SELECT DepartmentID,
           Clean = LTRIM(RTRIM(DepartmentName)),
           rn = ROW_NUMBER() OVER (PARTITION BY LOWER(LTRIM(RTRIM(DepartmentName))) ORDER BY DepartmentID)
    FROM dbo.Departments
)
UPDATE d
SET DepartmentName = c.Clean + N' (' + CONVERT(NVARCHAR(10), c.rn) + N')'
FROM dbo.Departments d
JOIN c ON d.DepartmentID = c.DepartmentID
WHERE c.rn > 1;

-- Mirror back to Name if different/empty
UPDATE dbo.Departments SET Name = DepartmentName WHERE Name IS NULL OR Name = N'';
", cn))
            {
                await cmd.ExecuteNonQueryAsync();
            }

            // Seed canonical set using BOTH columns (avoids NOT NULL on legacy 'Name')
            using (var seed = new SqlCommand(@"
WITH wanted(n) AS
(
    SELECT N'Emergency' UNION ALL
    SELECT N'Internal Medicine' UNION ALL
    SELECT N'Surgery' UNION ALL
    SELECT N'Radiology' UNION ALL
    SELECT N'HR'
)
INSERT INTO dbo.Departments(DepartmentName, Name)
SELECT w.n, w.n
FROM wanted w
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Departments d
    WHERE LOWER(LTRIM(RTRIM(COALESCE(d.DepartmentName, d.Name)))) = LOWER(LTRIM(RTRIM(w.n)))
);", cn))
            {
                try { await seed.ExecuteNonQueryAsync(); } catch { /* ignore */ }
            }

            // Make sure there are no NULLs left after seeding (for strict schemas)
            using (var final = new SqlCommand(@"
UPDATE d
SET DepartmentName = ISNULL(DepartmentName, N'General'),
    Name           = ISNULL(Name,           ISNULL(DepartmentName, N'General'))
FROM dbo.Departments d;", cn))
            {
                await final.ExecuteNonQueryAsync();
            }
        }

        // ---------- Utility helpers ----------

        private static async Task<bool> ColumnExistsAsync(SqlConnection cn, string schema, string table, string column)
        {
            const string sql = @"
SELECT 1
FROM sys.columns c
JOIN sys.tables  t ON c.object_id = t.object_id
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name=@sch AND t.name=@tab AND c.name=@col;";
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.AddWithValue("@sch", schema);
                cmd.Parameters.AddWithValue("@tab", table);
                cmd.Parameters.AddWithValue("@col", column);
                var o = await cmd.ExecuteScalarAsync();
                return o != null;
            }
        }

        // Optional helper used elsewhere
        public static async Task EnsureUsersEmailColumnAsync(SqlConnection cn)
        {
            const string sql = @"
IF OBJECT_ID('dbo.Users','U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users','Email') IS NULL
        ALTER TABLE dbo.Users ADD Email NVARCHAR(100) NULL;
END";
            using (var cmd = new SqlCommand(sql, cn))
                await cmd.ExecuteNonQueryAsync();
        }

        // Simple debug
        public static async Task<string> DebugCheckAsync()
        {
            using (var cn = new SqlConnection(connectionString))
            {
                await cn.OpenAsync();
                var sb = new StringBuilder();

                using (var cmd = new SqlCommand("SELECT DB_NAME()", cn))
                    sb.AppendLine("DB=" + (string)await cmd.ExecuteScalarAsync());

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name='Staffs' AND schema_id = SCHEMA_ID('dbo');", cn))
                    sb.AppendLine("dbo.Staffs exists=" + Convert.ToInt32(await cmd.ExecuteScalarAsync()));

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name='Departments' AND schema_id = SCHEMA_ID('dbo');", cn))
                    sb.AppendLine("dbo.Departments exists=" + Convert.ToInt32(await cmd.ExecuteScalarAsync()));

                return sb.ToString();
            }
        }
    }
}
