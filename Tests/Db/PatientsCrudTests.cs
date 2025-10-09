using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using HospitalManagementSystem.Tests.Infrastructure;

namespace HospitalManagementSystem.Tests.Db
{
    [TestClass, TestCategory("Integration-DB")]
    public class PatientsCrudTests : BaseIntegrationTest
    {
        [TestMethod]
        public void Insert_And_ReadBack_Adaptive()
        {
            const string T = "Patients";
            using var con = new SqlConnection(Cs);
            con.Open();
            Assert.IsTrue(TableExists(con, T), "Patients table missing");

            string idCol = FirstExisting(con, T, "PatientID", "PatientId", "Id");
            string firstCol = FirstExisting(con, T, "FirstName", "FName", "GivenName", "PatientFirstName", "Name");
            string lastCol = FirstExisting(con, T, "LastName", "LName", "Surname", "PatientLastName");
            string dobCol = FirstExisting(con, T, "DateOfBirth", "DOB", "BirthDate", "DoB");

            Assert.IsNotNull(idCol); Assert.IsNotNull(firstCol); Assert.IsNotNull(lastCol);

            var cols = new List<string> { firstCol, lastCol };
            var vals = new List<string> { "@fn", "@ln" };
            var ps = new List<SqlParameter>{
                new("@fn","Test"),
                new("@ln","Patient")
            };
            if (dobCol != null) { cols.Add(dobCol); vals.Add("@dob"); ps.Add(new("@dob", new DateTime(2000, 1, 1))); }

            // one-shot materialize: required NOT NULL cols + types
            var required = GetRequiredNoDefaultColumnsWithTypes(con, T);
            foreach (var col in required)
            {
                if (Eq(col.Name, idCol) || Eq(col.Name, firstCol) || Eq(col.Name, lastCol) || (dobCol != null && Eq(col.Name, dobCol)))
                    continue;
                string p = "@p_" + col.Name;
                cols.Add(col.Name);
                vals.Add(p);
                ps.Add(new SqlParameter(p, DummyForType(col.SqlType)));
            }

            int newId;
            using (var cmd = new SqlCommand(
                $"INSERT INTO dbo.{T} ({string.Join(",", cols)}) OUTPUT INSERTED.{idCol} VALUES ({string.Join(",", vals)});", con))
            { cmd.Parameters.AddRange(ps.ToArray()); newId = Convert.ToInt32(cmd.ExecuteScalar()); }

            using (var cmd = new SqlCommand($"SELECT {firstCol},{lastCol} FROM dbo.{T} WHERE {idCol}=@id", con))
            {
                cmd.Parameters.AddWithValue("@id", newId);
                using var r = cmd.ExecuteReader();
                Assert.IsTrue(r.Read());
                Assert.AreEqual("Test", r.GetString(0));
                Assert.AreEqual("Patient", r.GetString(1));
            }
        }

        // helpers
        private static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        private static string FirstExisting(SqlConnection con, string t, params string[] cands)
        { foreach (var c in cands) if (ColExists(con, t, c)) return c; return null; }

        private static bool ColExists(SqlConnection con, string t, string c)
        {
            const string sql = @"SELECT 1 FROM sys.columns c JOIN sys.tables tb ON tb.object_id=c.object_id WHERE tb.name=@t AND c.name=@c";
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@t", t);
            cmd.Parameters.AddWithValue("@c", c);
            return cmd.ExecuteScalar() != null;
        }

        private sealed class ColInfo { public string Name; public string SqlType; }

        private static List<ColInfo> GetRequiredNoDefaultColumnsWithTypes(SqlConnection con, string t)
        {
            const string sql = @"
;WITH meta AS (
  SELECT c.name ColName, ty.name TypeName, c.is_nullable, c.is_computed,
         COLUMNPROPERTY(c.object_id,c.name,'IsIdentity') AS IsIdentity,
         dc.object_id DefaultObjectId
  FROM sys.columns c
  JOIN sys.tables  tb ON tb.object_id=c.object_id
  JOIN sys.types   ty ON ty.user_type_id=c.user_type_id
  LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id=c.object_id AND dc.parent_column_id=c.column_id
  WHERE tb.name=@t
)
SELECT ColName, TypeName
FROM meta
WHERE is_nullable=0 AND (IsIdentity=0 OR IsIdentity IS NULL) AND is_computed=0 AND DefaultObjectId IS NULL;";
            var list = new List<ColInfo>();
            using var cmd = new SqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@t", t);
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(new ColInfo { Name = r.GetString(0), SqlType = r.GetString(1) });
            return list;
        }

        private static object DummyForType(string t)
        {
            t = (t ?? "").ToLowerInvariant();
            if (t.Contains("bit")) return true;
            if (t.Contains("int")) return 0;
            if (t.Contains("uniqueidentifier")) return Guid.NewGuid();
            if (t.Contains("date") || t.Contains("time")) return DateTime.UtcNow;
            if (t.Contains("decimal") || t.Contains("numeric") || t.Contains("money") || t.Contains("float") || t.Contains("real")) return 0;
            if (t.Contains("binary") || t.Contains("varbinary") || t.Contains("image")) return new byte[0];
            return "Test";
        }
    }
}
