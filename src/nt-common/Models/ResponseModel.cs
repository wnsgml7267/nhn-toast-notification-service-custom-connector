namespace Toast.Common.Models
{
    /// <summary>
    /// This represents the model entity for response.
    /// </summary>
    public abstract class ResponseModel
    {
        /// <summary>
        /// Gets or sets the header.
        /// </summary>
        public virtual ResponseHeaderModel Header { get; set; } = new ResponseHeaderModel();
    }
    
    /// <summary>
    /// This represents the model entity for response.
    /// </summary>
    /// <typeparam name="T">Type of model entity representing the response body.</typeparam>
    public abstract class ResponseModel<T> : ResponseModel
    {
        /// <summary>
        /// Gets or sets the body.
        /// </summary>
        public virtual T Body { get; set; }
    }
}