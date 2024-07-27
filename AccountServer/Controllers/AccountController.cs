using AccountServer.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        AppDbContext _context;

        public AccountController(AppDbContext db)
        {
            _context = db;
        }

        [HttpPost]
        [Route("Create")]
        public CreateAccountPacketRes CreateAccount([FromBody] CreateAccountPacketReq req)
        {
            CreateAccountPacketRes res = new CreateAccountPacketRes();
            AccountDb? account =  _context.Accounts
                                        .AsNoTracking()
                                        .Where(a => a.AccountName == req.AccountName)
                                        .FirstOrDefault();
            if (account == null)
            {
                _context.Accounts.Add(new AccountDb()
                {
                    AccountName = req.AccountName,
                    Password = req.Password
                });
                bool success = _context.SaveChangesEx();
                res.CreateOk = success;
            }
            else
            {
                res.CreateOk = false;
            }

            return res;
        }

        [HttpPost]
        [Route("Login")]
        public LoginAccountPacketRes LoginAccount([FromBody] LoginAccountPacketReq req)
        {
            LoginAccountPacketRes res = new LoginAccountPacketRes();

            AccountDb? account = _context.Accounts
                                        .AsNoTracking()
                                        .Where(a => a.AccountName == req.AccountName && a.Password == req.Password)
                                        .FirstOrDefault();

            if(account == null)
            {
                res.LoginOk = false;
            }
            else
            {
                res.LoginOk = true;

                //TODO 서버 목록?
                res.ServerList = new List<ServerInfo>()
                {
                    new ServerInfo() {Name = "바다" , Ip="127.0.0.1", CrowdeLevel = 0 },
                    new ServerInfo() {Name = "산" , Ip="127.0.0.1", CrowdeLevel = 3 }
                };
            }
            return res;
        }

        
    }
}
