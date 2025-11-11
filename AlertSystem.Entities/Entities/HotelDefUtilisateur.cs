namespace AlertSystem.Entities.Entities
{
    /// <summary>
    /// Entité pour la table hotel def_utilisateur (read-only)
    /// Renommé depuis DefUtilisateur pour éviter les conflits avec la nouvelle table def_Utilisateur
    /// </summary>
    public sealed class HotelDefUtilisateur
    {
        public decimal util_id { get; set; }
        public short profile_id { get; set; }
        public decimal? tpv_plan_touche_id { get; set; }
        public short? hotel_id { get; set; }
        public decimal? grh_emp_id { get; set; }
        public string util_nom { get; set; } = string.Empty;
        public string util_prenom { get; set; } = string.Empty;
        public string util_login { get; set; } = string.Empty;
        public string util_password { get; set; } = string.Empty;
        public string util_fonction { get; set; } = string.Empty;
        public string? util_email { get; set; }
        public DateTime? util_date_expiration_mdp { get; set; }
        public bool util_compte_active { get; set; } = true;
        public bool util_get_fond_caisse { get; set; } = false;
        public string? util_code_operateur { get; set; }
        public string? util_work_station { get; set; }
        public string? util_user_login { get; set; }
        public int langue_id { get; set; } = 2;
        public string? util_signature_name { get; set; }
        public string? util_signature { get; set; }
    }
}

