using IService.BaseManage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections;
using System.IO;
using Tools.Base;

namespace WebApp.Controllers
{
    /// <summary>
    /// 用户选择器
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : Controller
    {
        #region DI(依赖注入)
        private readonly IPersonService _personinfo;//人员合同信息
        private readonly IECPersonFileInfoService _fileinfo;//人员合同信息

        public PersonController(IPersonService personinfo, IECPersonFileInfoService fileinfo)
        {
            _personinfo = personinfo;
            _fileinfo = fileinfo;
        }
        #endregion
        
        #region 查询接口
        /// <summary>
        /// 获取人员附件信息
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPersonFileInfo")]
        public ActionResult<object> GetPersonFileInfo(string idCard)
        {
            var UserFileInfo = _fileinfo.GetPersonFileInfo(idCard);

            if (UserFileInfo != null)
            {
                var result = new { code = 0, msg = "成功",  data = UserFileInfo };
                return result.ToJson();
            }
            else
            {
                var result = new { code = -1, msg = "未查询到数据", count = 0, data = "" };
                return result.ToJson();
            }
        }
        #endregion

        #region 保存接口
        /// <summary>
        /// 获取人员附件信息
        /// </summary>
        /// <returns></returns>
        [HttpPost("SaveFileInfo")]
        public ActionResult<object> SaveFileInfo([FromBody] object pramJson)
        {
            var UserFileInfo = _fileinfo.SaveFileInfo(pramJson.ToString());

            if (UserFileInfo == "true")
            {
                var result = new { code = 0, msg = "成功" };
                return result.ToJson();
            }
            else
            {
                var result = new { code = -1, msg = "未查询到数据" };
                return result.ToJson();
            }
        }
        #endregion

        #region 附件上传接口
        [HttpPost("UploadFile")]
        public ActionResult<object> UploadFile()
        {
            if (Request.Form.Files.Count > 0)
            {
                if (Request.Form.Files[0].Length > 0)
                {
                    IFormFile file = Request.Form.Files[0];

                    string InitialFileName = Request.Form.Files[0].FileName;
                    string extension = Path.GetExtension(InitialFileName)?.ToLower();
                    string fileName = Guid.NewGuid().ToString() + extension;
                    string basePath = @"D:\DH\DHWeb\Content\Template\ECFiles";//直接使用OA系统根目录
                    var filePath = FileBase64.CreateFilePath(basePath, fileName);
                    var returnpath = Path.Combine("Content//Template//ECFiles//", filePath[0]);
                    string path = Path.Combine(basePath, filePath[0]);
                    using (var stream = System.IO.File.Create(path))
                    {
                        #region 插入
                        try
                        {
                            file.CopyTo(stream);
                        }
                        catch (Exception e)
                        {

                            throw e;
                        }
                        #endregion
                    }

                    var data = new Hashtable
                    {
                        { "FilePath", returnpath },
                        { "FileName", InitialFileName }
                    };

                    var result = new { code = 0, msg = "成功", data = data };
                    return result.ToJson();
                }
                else
                {
                    var result = new { code = -1, msg = "上传的文件大小为0，请检查！" };
                    return result.ToJson();
                }
            }
            else
            {
                var result = new { code = -1, msg = "未检查到上传的文件" };
                return result.ToJson();
            }
        } 
        #endregion
    }
}