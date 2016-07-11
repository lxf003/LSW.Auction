using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using 拍卖系统.Data;
using LSW.Weixin.MP.Entities.Request;
using LSW.Weixin.MP;
using System.Text;
using System.IO;
using LSW.Weixin.MP.MvcExtension;

namespace 拍卖系统.Controllers
{
    public class WeixinController : ControllerBase
    {
		public WeixinController(ApplicationDbContext context) : base(context) { }
		/// <summary>
		/// 微信后台验证地址（使用Get），微信后台的“接口配置信息”的Url填写如：http://wx.car0774.com/weixin
		/// </summary>
		[HttpGet]
		[ActionName("Index")]
		public ActionResult Get(PostModel postModel, string echostr)
		{
			if (CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
			{
				return Content(echostr);//返回随机字符串则表示验证通过
			}
			else
			{
				return Content("failed:" + postModel.Signature + "," + CheckSignature.GetSignature(postModel.Timestamp, postModel.Nonce, Token) + "。如果你在浏览器中看到这句话，说明此地址可以被作为微信公众账号后台的Url，请注意保持Token一致。");
			}
		}
		/// <summary>
		/// 用户发送消息后，微信平台自动Post一个请求到这里，并等待响应XML
		/// </summary>
		[HttpPost]
		[ActionName("Index")]
		public ActionResult Post(PostModel postModel)
		{
			if (!CheckSignature.Check(postModel.Signature, postModel.Timestamp, postModel.Nonce, Token))
				return Content("参数错误！");

			postModel.Token = Token;
			postModel.EncodingAESKey = EncodingAESKey;//根据自己后台的设置保持一致
			postModel.AppId = AppId;//根据自己后台的设置保持一致

			string body = new StreamReader(Request.Body).ReadToEnd();           // log body

			byte[] requestData = Encoding.UTF8.GetBytes(body);
			Stream inputstream = new MemoryStream(requestData);

			var messageHandler = new CustomMessageHandler(db, inputstream, postModel);//接收消息

			messageHandler.Execute();//执行微信处理过程

			//return Content(messageHandler.ResponseDocument.ToString());//v0.7-
			//return new WeixinResult(messageHandler);//v0.8+ with MvcExtension
			return new FixWeixinBugWeixinResult(messageHandler);//为了解决官方微信5.0以后软件换行bug暂时添加的方法，平时用上面一个方法即可
		}
	}
}