using System.Collections;

namespace TP3.Messages;

/// <summary>
/// Converts stream of tp3 messages into a stream of bytes. This is useful for reading data from a TP3ReadResponse stream.
/// </summary>
public class TP3StreamFromReadResponse : Stream
{
    private Func<IEnumerable<TP3ReadResponse>> ResponseProvider;
    private IEnumerator<TP3ReadResponse> _enumartor = null!;

    public override bool CanRead =>  ResponseProvider != null;
    
    public override bool CanSeek => throw new NotImplementedException();

    public override bool CanWrite => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        while (count > 0 && Tp3Enumartor.MoveNext())
        {
            var response = Tp3Enumartor.Current;
            if (response.Data != null)
            {
                var bytesToCopy = Math.Min(count, response.Data.Length);
                Array.Copy(response.Data, 0, buffer, offset, bytesToCopy);
                return bytesToCopy;
            }
        }
        return 0;
    }

    public IEnumerator<TP3ReadResponse> Tp3Enumartor
    {
        get
        {
            if(this._enumartor != null)
            {
                return this._enumartor;
            }
            
            var responses = ResponseProvider();
            this._enumartor = responses.GetEnumerator();

            return _enumartor;
        }
    }


    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public TP3StreamFromReadResponse(Func<IEnumerable<TP3ReadResponse>> responseProvider, ulong maxbytes = 0)
    {
        this.ResponseProvider = responseProvider;
        
    }
}