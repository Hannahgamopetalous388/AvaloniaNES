using AvaloniaNES.Device.Cart;

namespace AvaloniaNES.Device.Mapper;

public interface IMapperService
{
    public void MapperInit(byte prgBanks, byte chrBanks);

    public void Reset();

    public bool irqState();

    public void irqClear();

    public void scanline();

    public MirroringType GetMirrorType();

    public bool CPUMapRead(ushort address, ref uint mapAddress, ref byte data);

    public bool CPUMapWrite(ushort address, ref uint mapAddress, byte data);

    public bool PPUMapRead(ushort address, ref uint mapAddress);

    public bool PPUMapWrite(ushort address, ref uint mapAddress);
}