using System.Data.Entity.Migrations;

namespace GridDomain.Balance.ReadModel.Migrations
{
    public partial class What : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.BusinessBalances", "UpdatesCount");
        }

        public override void Down()
        {
            AddColumn("dbo.BusinessBalances", "UpdatesCount", c => c.Int(false));
        }
    }
}