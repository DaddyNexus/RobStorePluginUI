# Robstore Plugin

# About
Robstore plugin UI pops up when someone starts the robbery
stores can be created via command or config
must have gun out to start
min and max money that is randomised that is also configable

# configuration
```
<?xml version="1.0" encoding="utf-8"?>
<RobstoreConfiguration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <MinExp>300</MinExp>
  <MaxExp>4000</MaxExp>
  <RobCooldownSeconds>300</RobCooldownSeconds>
  <RobCheckIntervalMs>200</RobCheckIntervalMs>
  <RobHoldTimeSeconds>15</RobHoldTimeSeconds>
  <IconImageUrl>https://example.com/denied.png</IconImageUrl>
  <RobberyAlertPermissionGroup>police</RobberyAlertPermissionGroup>
  <StoreList>
    <StoreData>
      <Name>Example Store</Name>
      <Position>
        <x>0</x>
        <y>0</y>
        <z>0</z>
      </Position>
      <Radius>10</Radius>
    </StoreData>
  </StoreList>
  <RobberyUIEffectID>22006</RobberyUIEffectID>
  <RobberyUIEffectKey>60</RobberyUIEffectKey>
</RobstoreConfiguration> 
```
This is for Unturned Store form application
