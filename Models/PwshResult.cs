namespace WebWrap.Models
{
    public class PwshResult : BaseModel
    {
        //status 0 = success, 1 = error, 2 = process exited
        public int Status { get; set; }
        public string Output { get; set; }
        public bool IsRunning { get; set; }

        public PwshResult(string RequestId)
        {
            this.Type = "pwshResult";
            this.RequestId = RequestId;
        }
    }
   
}
