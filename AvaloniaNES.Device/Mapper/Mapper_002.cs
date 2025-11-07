using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

public class Mapper_002 : IMapperService
{
    private byte _prgBank;
    private byte _chrBank;
    private byte _prgBankSelect;
    private byte _prgBankFix;
    
    public void MapperInit(byte prgBanks, byte chrBanks)
    {
        _prgBank = prgBanks;
        _chrBank = chrBanks;
        Reset();
    }

    public void Reset()
    {
        _prgBankSelect = 0;
        _prgBankFix = (byte)(_prgBank - 1);
    }

    public MirroringType GetMirrorType()
    {
        return MirroringType.Hardware;
    }

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data)
    {
        if (address is >= 0x8000 and <= 0xBFFF)
        {
            mapAddress = (uint)(_prgBankSelect * 0x4000 + (address & 0x3FFF));
            return true;
        }
        if (address >= 0xC000)  // last 16kb is fixed
        {
            mapAddress = (uint)(_prgBankFix * 0x4000 + (address & 0x3FFF));
            return true;
        }

        return false;
    }

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data)
    {
        if (address is >= 0x8000 and < 0xFFFF)
        {
            _prgBankSelect = (byte)(data & 0x0F); // register
        }
        return false;
    }

    public bool PPUMapRead(ushort address, ref uint mapAddress)
    {
        if (address <= 0x1FFF)
        {
            mapAddress = address;
            return true;
        }
        return false;
    }

    public bool PPUMapWrite(ushort address, ref uint mapAddress)
    {
        if (address <= 0x1FFF)
        {
            if (_chrBank == 0)
            {
                // Treat as RAM
                mapAddress = address;
                return true;
            }
        }
        return false;
    }
}