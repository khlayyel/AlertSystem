using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using AlertSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AlertSystem.Worker.Services;

public interface IAlertWriter
{
    Task InsertAlertsAsync(ApplicationDbContext db,
        int? appId,
        int alertTypeId,
        string title,
        string description,
        int statutPendingId,
        int plateformeId,
        IEnumerable<Recipient> recipients);
}

public sealed class AlertWriter : IAlertWriter
{
    public async Task InsertAlertsAsync(ApplicationDbContext db,
        int? appId,
        int alertTypeId,
        string title,
        string description,
        int statutPendingId,
        int plateformeId,
        IEnumerable<Recipient> recipients)
    {
        var groupId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var sql = @"INSERT INTO Alerte (
                        AlertGroupId, AlertTypeId, AppId, ExpediteurId,
                        TitreAlerte, DescriptionAlerte, DateCreationAlerte,
                        StatutId, EtatAlerteId, PlateformeEnvoieId,
                        DestinataireUserId, DestinataireEmail, DestinatairePhoneNumber,
                        ProcessedByWorker, AttemptCount)
                    VALUES (
                        @groupId, @typeId, @appId, NULL,
                        @title, @body, @now,
                        @statut, 1, @platform,
                        @uid, @email, @phone,
                        0, 0);";

        using var conn = db.Database.GetDbConnection();
        if (conn.State != ConnectionState.Open) await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        foreach (var r in recipients)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = (System.Data.Common.DbTransaction)tx;
            cmd.CommandText = sql;
            var p = cmd.CreateParameter();
            p.ParameterName = "@groupId"; p.Value = groupId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@typeId"; p.Value = alertTypeId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@appId"; p.Value = (object?)appId ?? DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@title"; p.Value = title; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@body"; p.Value = (object?)description ?? DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@now"; p.Value = now; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@statut"; p.Value = statutPendingId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@platform"; p.Value = plateformeId; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@uid"; p.Value = (object?)r.UserId ?? DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@email"; p.Value = (object?)r.Email ?? DBNull.Value; cmd.Parameters.Add(p);
            p = cmd.CreateParameter(); p.ParameterName = "@phone"; p.Value = (object?)r.Phone ?? DBNull.Value; cmd.Parameters.Add(p);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }
}


