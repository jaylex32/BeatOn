[void][System.Reflection.Assembly]::LoadWithPartialName('presentationframework')

# Place xaml code from visual studio in here string (in the blank line between @ symbols)
$input = @’
<Window x:Name="d_fi_Front_End1" x:Class="WpfApp1.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="namespace:WpfApp1"
        mc:Ignorable="d"
        Title="d-fi Front End" Height="600" Width="900" MaxHeight="600" MaxWidth="925" MinHeight="600" MinWidth="925">
    <Window.Resources>
        <SolidColorBrush x:Key="TextBox.Static.Border" Color="#FFABAdB3"/>
        <SolidColorBrush x:Key="TextBox.MouseOver.Border" Color="#FFFF1700"/>
        <SolidColorBrush x:Key="TextBox.Focus.Border" Color="#FFFF1700"/>
        <Style x:Key="TextBoxStyle1" TargetType="{x:Type TextBox}">
            <Setter Property="Background" Value="{DynamicResource {x:Static SystemColors.WindowBrushKey}}"/>
            <Setter Property="BorderBrush" Value="{StaticResource TextBox.Static.Border}"/>
            <Setter Property="Foreground" Value="{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="KeyboardNavigation.TabNavigation" Value="None"/>
            <Setter Property="HorizontalContentAlignment" Value="Left"/>
            <Setter Property="AllowDrop" Value="true"/>
            <Setter Property="ScrollViewer.PanningMode" Value="VerticalFirst"/>
            <Setter Property="Stylus.IsFlicksEnabled" Value="False"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type TextBox}">
                        <Border x:Name="border" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" SnapsToDevicePixels="True">
                            <ScrollViewer x:Name="PART_ContentHost" Focusable="false" HorizontalScrollBarVisibility="Hidden" VerticalScrollBarVisibility="Hidden"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsEnabled" Value="false">
                                <Setter Property="Opacity" TargetName="border" Value="0.56"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="true">
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource TextBox.MouseOver.Border}"/>
                            </Trigger>
                            <Trigger Property="IsKeyboardFocused" Value="true">
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource TextBox.Focus.Border}"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
            <Style.Triggers>
                <MultiTrigger>
                    <MultiTrigger.Conditions>
                        <Condition Property="IsInactiveSelectionHighlightEnabled" Value="true"/>
                        <Condition Property="IsSelectionActive" Value="false"/>
                    </MultiTrigger.Conditions>
                    <Setter Property="SelectionBrush" Value="{DynamicResource {x:Static SystemColors.InactiveSelectionHighlightBrushKey}}"/>
                </MultiTrigger>
            </Style.Triggers>
        </Style>
        <Style x:Key="FocusVisual">
            <Setter Property="Control.Template">
                <Setter.Value>
                    <ControlTemplate>
                        <Rectangle Margin="2" StrokeDashArray="1 2" Stroke="{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}" SnapsToDevicePixels="true" StrokeThickness="1"/>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
        <SolidColorBrush x:Key="Button.Static.Background" Color="#FFDDDDDD"/>
        <SolidColorBrush x:Key="Button.Static.Border" Color="#FF707070"/>
        <SolidColorBrush x:Key="Button.MouseOver.Background" Color="#FFD2493C"/>
        <SolidColorBrush x:Key="Button.MouseOver.Border" Color="#FFFF1700"/>
        <SolidColorBrush x:Key="Button.Pressed.Background" Color="#FFD2493C"/>
        <SolidColorBrush x:Key="Button.Pressed.Border" Color="#FFFF1700"/>
        <SolidColorBrush x:Key="Button.Disabled.Background" Color="#FFF4F4F4"/>
        <SolidColorBrush x:Key="Button.Disabled.Border" Color="#FFADB2B5"/>
        <SolidColorBrush x:Key="Button.Disabled.Foreground" Color="#FF838383"/>
        <Style x:Key="ButtonStyle1" TargetType="{x:Type Button}">
            <Setter Property="FocusVisualStyle" Value="{StaticResource FocusVisual}"/>
            <Setter Property="Background" Value="{StaticResource Button.Static.Background}"/>
            <Setter Property="BorderBrush" Value="{StaticResource Button.Static.Border}"/>
            <Setter Property="Foreground" Value="{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="HorizontalContentAlignment" Value="Center"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
            <Setter Property="Padding" Value="1"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type Button}">
                        <Border x:Name="border" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" SnapsToDevicePixels="true">
                            <ContentPresenter x:Name="contentPresenter" Focusable="False" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" Margin="{TemplateBinding Padding}" RecognizesAccessKey="True" SnapsToDevicePixels="{TemplateBinding SnapsToDevicePixels}" VerticalAlignment="{TemplateBinding VerticalContentAlignment}"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsDefaulted" Value="true">
                                <Setter Property="BorderBrush" TargetName="border" Value="{DynamicResource {x:Static SystemColors.HighlightBrushKey}}"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="true">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.MouseOver.Background}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.MouseOver.Border}"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="true">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.Pressed.Background}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.Pressed.Border}"/>
                            </Trigger>
                            <Trigger Property="IsEnabled" Value="false">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.Disabled.Background}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.Disabled.Border}"/>
                                <Setter Property="TextElement.Foreground" TargetName="contentPresenter" Value="{StaticResource Button.Disabled.Foreground}"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
        <Style x:Key="FocusVisual1">
            <Setter Property="Control.Template">
                <Setter.Value>
                    <ControlTemplate>
                        <Rectangle Margin="2" StrokeDashArray="1 2" Stroke="{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}" SnapsToDevicePixels="true" StrokeThickness="1"/>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
        <SolidColorBrush x:Key="Button.Static.Background1" Color="#FFDDDDDD"/>
        <SolidColorBrush x:Key="Button.Static.Border1" Color="#FF707070"/>
        <SolidColorBrush x:Key="Button.MouseOver.Background1" Color="#FFD2493C"/>
        <SolidColorBrush x:Key="Button.MouseOver.Border1" Color="#FFFF1700"/>
        <SolidColorBrush x:Key="Button.Pressed.Background1" Color="#FFD2493C"/>
        <SolidColorBrush x:Key="Button.Pressed.Border1" Color="#FFFF1700"/>
        <SolidColorBrush x:Key="Button.Disabled.Background1" Color="#FFF4F4F4"/>
        <SolidColorBrush x:Key="Button.Disabled.Border1" Color="#FFADB2B5"/>
        <SolidColorBrush x:Key="Button.Disabled.Foreground1" Color="#FF838383"/>
        <Style x:Key="ButtonStyle2" TargetType="{x:Type Button}">
            <Setter Property="FocusVisualStyle" Value="{StaticResource FocusVisual1}"/>
            <Setter Property="Background" Value="{StaticResource Button.Static.Background1}"/>
            <Setter Property="BorderBrush" Value="{StaticResource Button.Static.Border1}"/>
            <Setter Property="Foreground" Value="{DynamicResource {x:Static SystemColors.ControlTextBrushKey}}"/>
            <Setter Property="BorderThickness" Value="1"/>
            <Setter Property="HorizontalContentAlignment" Value="Center"/>
            <Setter Property="VerticalContentAlignment" Value="Center"/>
            <Setter Property="Padding" Value="1"/>
            <Setter Property="Template">
                <Setter.Value>
                    <ControlTemplate TargetType="{x:Type Button}">
                        <Border x:Name="border" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" SnapsToDevicePixels="true">
                            <ContentPresenter x:Name="contentPresenter" Focusable="False" HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" Margin="{TemplateBinding Padding}" RecognizesAccessKey="True" SnapsToDevicePixels="{TemplateBinding SnapsToDevicePixels}" VerticalAlignment="{TemplateBinding VerticalContentAlignment}"/>
                        </Border>
                        <ControlTemplate.Triggers>
                            <Trigger Property="IsDefaulted" Value="true">
                                <Setter Property="BorderBrush" TargetName="border" Value="{DynamicResource {x:Static SystemColors.HighlightBrushKey}}"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="true">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.MouseOver.Background1}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.MouseOver.Border1}"/>
                            </Trigger>
                            <Trigger Property="IsPressed" Value="true">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.Pressed.Background1}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.Pressed.Border1}"/>
                            </Trigger>
                            <Trigger Property="IsEnabled" Value="false">
                                <Setter Property="Background" TargetName="border" Value="{StaticResource Button.Disabled.Background1}"/>
                                <Setter Property="BorderBrush" TargetName="border" Value="{StaticResource Button.Disabled.Border1}"/>
                                <Setter Property="TextElement.Foreground" TargetName="contentPresenter" Value="{StaticResource Button.Disabled.Foreground1}"/>
                            </Trigger>
                        </ControlTemplate.Triggers>
                    </ControlTemplate>
                </Setter.Value>
            </Setter>
        </Style>
    </Window.Resources>
    <Window.RenderTransform>
        <TransformGroup>
            <ScaleTransform/>
            <SkewTransform/>
            <RotateTransform/>
            <TranslateTransform X="0"/>
        </TransformGroup>
    </Window.RenderTransform>
    <Window.Background>
        <LinearGradientBrush EndPoint="0.5,1" StartPoint="0.5,0">
            <GradientStop Color="Black"/>
            <GradientStop Color="#FF3A3A3A"/>
            <GradientStop Color="#FF7B7B7B"/>
            <GradientStop Color="White"/>
            <GradientStop Color="#FF4D4D4D"/>
            <GradientStop/>
            <GradientStop Color="#E8868686"/>
            <GradientStop Color="#EE000000"/>
            <GradientStop Color="#EE272525" Offset="0.14"/>
            <GradientStop Color="#EE100E0E" Offset="0.537"/>
            <GradientStop Color="#EE000000" Offset="0.653"/>
            <GradientStop Color="#EE000000" Offset="0.99"/>
            <GradientStop Color="#EE333030" Offset="0.253"/>
        </LinearGradientBrush>
    </Window.Background>
    <Grid Width="900" Margin="0,0,0,-16" Height="600">
        <Grid.Background>
            <LinearGradientBrush EndPoint="0.5,1" Opacity="0.265" StartPoint="0.5,0">
                <GradientStop Color="Black" Offset="0"/>
                <GradientStop Color="Black" Offset="0.2"/>
                <GradientStop Color="Black" Offset="0.54"/>
                <GradientStop Color="#FF252525" Offset="1"/>
                <GradientStop Color="#FF1F1C1C"/>
                <GradientStop Color="Black"/>
                <GradientStop Color="#FF020101" Offset="0.363"/>
            </LinearGradientBrush>
        </Grid.Background>
        <Grid.OpacityMask>
            <LinearGradientBrush EndPoint="0.5,1" StartPoint="0.5,0">
                <GradientStop Color="Black"/>
                <GradientStop Color="White" Offset="0.877"/>
                <GradientStop Color="#FFE22222" Offset="0.527"/>
                <GradientStop Color="#FF040404" Offset="0.423"/>
                <GradientStop Color="#FFD21F1F" Offset="0.62"/>
            </LinearGradientBrush>
        </Grid.OpacityMask>
        <Label x:Name="d_fi_Front_End" Content="d-fi Front-End" HorizontalAlignment="Center" Margin="0,48,0,0" VerticalAlignment="Top" FontSize="24" FontFamily="Impact" Width="140" FontWeight="Regular" FontStyle="Normal" FontStretch="Normal" Height="40">
            <Label.Foreground>
                <LinearGradientBrush EndPoint="0.5,1" StartPoint="0.5,0">
                    <GradientStop Color="#FF2E2626" Offset="0.73"/>
                    <GradientStop Color="#FF7D7D7D" Offset="0.267"/>
                    <GradientStop Color="#FFF3F3F3" Offset="0.02"/>
                    <GradientStop Color="Red" Offset="1"/>
                    <GradientStop Color="#FF231818" Offset="0.09"/>
                </LinearGradientBrush>
            </Label.Foreground>
            <Label.Background>
                <LinearGradientBrush EndPoint="0.5,1" StartPoint="0.5,0">
                    <GradientStop Color="Black" Offset="0.103"/>
                    <GradientStop Color="White" Offset="0.477"/>
                    <GradientStop Color="#FF1B1B1B" Offset="0.907"/>
                    <GradientStop Color="#FF323232" Offset="0.963"/>
                </LinearGradientBrush>
            </Label.Background>
        </Label>
        <TextBox Style="{DynamicResource TextBoxStyle1}" x:Name="Search_Box" HorizontalAlignment="Left" Margin="184,122,0,0" TextWrapping="Wrap" Text="" VerticalAlignment="Top" VerticalContentAlignment="Center" Width="472" Height="30" FontSize="14" BorderThickness="0.9,0.9,0.9,0.9" SelectionBrush="#FFE25151">
            <TextBox.Resources>
                <Style TargetType="{x:Type Border}">
                    <Setter Property="CornerRadius" Value="3"/>
                </Style>
            </TextBox.Resources>
        </TextBox>
        <Button Style="{DynamicResource ButtonStyle1}" x:Name="Search_Button" HorizontalAlignment="Left" Content="Search" Margin="658,122,0,0" VerticalAlignment="Top" Width="58" Height="30" FontFamily="Yu Gothic UI Semibold" FontSize="14" BorderBrush="#00707070" BorderThickness="0.5" IsDefault="True">
            <Button.Resources>
                <Style TargetType="{x:Type Border}">
                    <Setter Property="CornerRadius" Value="0"/>
                </Style>
            </Button.Resources>
        </Button>
        <RadioButton x:Name="ArtistRadiobutton" GroupName="DownloadOptions" Content="Artist" HorizontalAlignment="Left" Margin="720,260,0,0" VerticalAlignment="Top" Foreground="#FFEDEDED" Width="52" FontWeight="Bold" FontFamily="Constantia" Height="18" RenderTransformOrigin="0.5,0.5"/>
        <RadioButton x:Name="SongsRadiobutton" GroupName="DownloadOptions" Content="Songs" HorizontalAlignment="Left" Margin="720,308,0,0" VerticalAlignment="Top" FontWeight="Bold" Foreground="White" Width="52" Height="18" FontFamily="Constantia"/>
        <RadioButton x:Name="AlbumsRadiobutton" GroupName="DownloadOptions" Content="Albums" HorizontalAlignment="Left" Margin="720,356,0,0" VerticalAlignment="Top" FontWeight="Bold" Foreground="White" Width="63" Height="18" FontFamily="Constantia"/>
        <RadioButton x:Name="PlaylistRadiobutton" GroupName="DownloadOptions" Content="Playlist" HorizontalAlignment="Left" Margin="720,404,0,0" VerticalAlignment="Top" FontWeight="Bold" Foreground="White" Width="63" Height="18" FontFamily="Constantia"/>
        <RadioButton x:Name="UrlRadiobutton" GroupName="StreamServices" Content="Spotify" HorizontalAlignment="Left" Margin="490,174,0,0" VerticalAlignment="Top" Foreground="White" FontWeight="Bold" Width="60" RenderTransformOrigin="0.18,0.404" Height="18" FontFamily="Constantia"/>
        <RadioButton x:Name="QobuzUrlRadiobutton" GroupName="StreamServices" Content="Qobuz" HorizontalAlignment="Left" Margin="350,174,0,0" VerticalAlignment="Top" Foreground="White" FontWeight="Bold" Width="60" RenderTransformOrigin="0.18,0.404" Height="18" FontFamily="Constantia"/>
        <RadioButton x:Name="DeezerUrlRadiobutton" GroupName="StreamServices" Content="Deezer" HorizontalAlignment="Center" Margin="0,174,0,0" VerticalAlignment="Top" Foreground="White" FontWeight="Bold" Width="60" RenderTransformOrigin="0.18,0.404" Height="18" FontFamily="Constantia"/>
        <Button Style="{DynamicResource ButtonStyle2}" x:Name="Downloadbutton" HorizontalAlignment="Left" Margin="720,122,0,0" VerticalAlignment="Top" Height="30" RenderTransformOrigin="-0.05,0.622" FontWeight="Normal" FontFamily="Bahnschrift SemiBold" FontSize="16" Width="40" ClickMode="Press" Focusable="False" BorderBrush="#00707070" BorderThickness="1,1,1,1" Background="#FFDDDDDD">
            <Button.Resources>
                <Style TargetType="{x:Type Border}">
                    <Setter Property="CornerRadius" Value="3,0,3,0"/>
                </Style>
            </Button.Resources>
            <StackPanel Orientation="Horizontal">
                <Image Source="https://github.com/jaylex32/athenastreamdownloader/blob/main/img/Download_Button.png?raw=true" Height="23" Width="35" Margin="0.7,0.5,0,0"/>
            </StackPanel>
        </Button>
        <Border BorderBrush="#FF6B6B6B" BorderThickness="0,0.5,1,0.5" Margin="184,210,184,372" Background="#FFDDDDDD" CornerRadius="5,5,0,0">
            <Label x:Name="AlbumLabel" Content="" HorizontalAlignment="Center" VerticalAlignment="Top" Foreground="Black" FontWeight="Bold" FontFamily="Cambria" Height="22" FontSize="12" RenderTransformOrigin="0.536,2.002" Width="Auto" Margin="0,-4,0,0"/>
        </Border>
        <ListBox x:Name="Albums_Listbox" Margin="184,228,185,106"></ListBox>
        <Image x:Name="Albums_Covers" HorizontalAlignment="Left" Height="167" Margin="37,275,0,0" VerticalAlignment="Top" Width="138"/>
        <Label x:Name="Poster_Label" HorizontalAlignment="Left" Margin="37,415,0,0" VerticalAlignment="Top" Width="138" Foreground="White">
         <Label.Content>
           <AccessText TextWrapping="Wrap" Text=""/>
         </Label.Content>
        </Label>
        <Button Style="{DynamicResource ButtonStyle2}" x:Name="Status_Button" Margin="10,200,5,50" HorizontalAlignment="Right" VerticalAlignment="Bottom" Height="31" Width="31" Foreground="White" BorderBrush="#16707070" Background="#FFBFBEBE">
        <Button.Resources>
        <Style TargetType="{x:Type Border}">
            <Setter Property="CornerRadius" Value="30"/>
        </Style>
    </Button.Resources>
    <StackPanel Orientation="Horizontal">
        <StackPanel.Background>
            <LinearGradientBrush EndPoint="0.5,1" StartPoint="0.5,0">
                <GradientStop Color="#00655630"/>
                <GradientStop Color="#00655630" Offset="0.653"/>
                <GradientStop Color="#0865B904" Offset="0.383"/>
            </LinearGradientBrush>
        </StackPanel.Background>
        <Image Source="https://github.com/jaylex32/athenastreamdownloader/blob/main/img/status.png?raw=true" Width="27" Margin="0.5,0,0,0"/>
    </StackPanel>
    </Button>
    </Grid>
</Window>
'@

$input = $input -replace '^<Window.*', '<Window' -replace 'mc:Ignorable="d"','' -replace "x:N",'N' 
[xml]$xaml = $input
$xmlreader=(New-Object System.Xml.XmlNodeReader $xaml)
$xamlForm=[Windows.Markup.XamlReader]::Load( $xmlreader )

$xaml.SelectNodes("//*[@Name]") | ForEach-Object -Process {
    Set-Variable -Name ($_.Name) -Value $xamlForm.FindName($_.Name)
    }

########this section is for the external settings.json#######
#------------------------------------------------------------

 $jsonFilepath="settings.json"

 $SettingsData=Get-Content -Path $jsonFilepath | ConvertFrom-Json

 $QMusicpath=$SettingsData.QPath 

 $Qobuzappid=$SettingsData.Qapp_id
 
 $QobuzSecrets=$SettingsData.Qsecrets

 $QobuzToken=$SettingsData.Qtoken

 $SpotMusicpath=$SettingsData.SpotPath
 
 $SpotLayoutMusic=$SettingsData.SpotLayout
 
 $SpotTracknumber=$SettingsData.trackNumber
 
 $QQualityFiles=$SettingsData.QQuality

 $QLayoutMusic=$SettingsData.QLayout

 $QTextFiles=$SettingsData.QTxt

 $DMusicpath=$SettingsData.Dpath

 $DQualityFiles=$SettingsData.DQuality

 $DLayoutMusic=$SettingsData.DLayout

 $DTextFiles=$SettingsData.DTxt

# code to run before button click event
#--------------------------------------

# code for Qobuz New Releases and Album of the Week
#--------------------------------------------------
function SearchNewmusic {
    if ($Search_Box.text -eq 'Release of the week'){
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?offset=0&offset=0&limit=50&limit=50&extra=track&format_id="
        $albumlist = Invoke-WebRequest -Uri "$BaseUri$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET -Headers $headers
        $responsealbumexplore = $albumlist | ConvertFrom-Json
        $explorecontent = $responsealbumexplore.containers.'container-re-release-of-the-week'
        $exploreitems = $explorecontent.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        foreach($Album in $exploreitems){  
           $AlbumResults = [PSCustomObject]@{
               Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(60, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length)))"
               QobuzUrl ="$($qobuzmainlink)$($album.id)"
               QobuzPoster ="$($Album.image.large)"
               Qposterlabel = "$($Album.artist.name) - $($Album.title)"
               Genrelabel = "RELEASE OF THE WEEK"
            }
           $Albums_list.Add($AlbumResults)
         }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Script:Albumsindex = $Albums_list
    }
    elseif ($Search_Box.text -eq 'Album of the week') 
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?offset=0&offset=0&limit=50&limit=50&extra=track&format_id="
        $albweeklist = Invoke-WebRequest -Uri "$BaseUri$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET -Headers $headers 
        $responsealbumweek = $albweeklist | ConvertFrom-Json
        $explore = $responsealbumweek.containers.'container-album-of-the-week'
        $exploreitems = $explore.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        foreach($Album in $exploreitems){
            $AlbweekResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(60, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "ALBUM OF THE WEEK"
            }
            $Albums_list.Add($AlbweekResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
    }
    elseif ($Search_Box.text -eq 'Album New Releases')
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?offset=0&offset=0&limit=50&limit=50&extra=track&format_id="
        $albweeklist = Invoke-WebRequest -Uri "$BaseUri$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET -Headers $headers 
        $responsealbumweek = $albweeklist | ConvertFrom-Json
        $explore = $responsealbumweek.containers.'container-album-new-releases'
        $exploreitems = $explore.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        foreach($Album in $exploreitems){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(60, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "ALBUM NEW RELEASES"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
    }
    elseif ($Search_Box.text -eq 'Rap') 
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $GenreRap = "genre_ids=133%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&"
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?"
        $RapGenreList = Invoke-WebRequest -Uri "$($BaseUri)$($GenreRap)format_id=$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET 
         
        $genrecontent= $RapGenreList.Content | ConvertFrom-Json 
        $rapnewreleases = $genrecontent.containers.'container-album-new-releases'
        $rapalbumnewreleases = $rapnewreleases.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        
        foreach($Album in $rapalbumnewreleases){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(60, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "RAP"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($Search_Box.text -eq 'Pop') 
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $GenrePop = "genre_ids=112%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&"
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?"
        $PopGenreList = Invoke-WebRequest -Uri "$($BaseUri)$($GenrePop)format_id=$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET 
         
        $genrecontent= $PopGenreList.Content | ConvertFrom-Json 
        $popnewreleases = $genrecontent.containers.'container-album-new-releases'
        $popalbumnewreleases = $popnewreleases.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        
        foreach($Album in $popalbumnewreleases){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(60, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "POP"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($Search_Box.text -eq 'Latin') 
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $GenreLatin = "genre_ids=149%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&"
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?"
        $LatinGenreList = Invoke-WebRequest -Uri "$($BaseUri)$($GenreLatin)format_id=$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET 
         
        $genrecontent= $LatinGenreList.Content | ConvertFrom-Json 
        $Latinnewreleases = $genrecontent.containers.'container-album-new-releases'
        $Latinalbumnewreleases = $Latinnewreleases.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        
        foreach($Album in $Latinalbumnewreleases){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(40, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(40, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "LATIN"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($Search_Box.text -eq 'Electro') 
    {
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $GenreElectro = "genre_ids=64%3Foffset%3D0&offset=0&limit=35&limit=35&extra=track&"
        $BaseUri = "https://www.qobuz.com/api.json/0.2/featured/index?"
        $ElectroGenreList = Invoke-WebRequest -Uri "$($BaseUri)$($GenreElectro)format_id=$QobuzToken&app_id=$Qobuzappid" -UseBasicParsing -Method GET 
         
        $genrecontent= $ElectroGenreList.Content | ConvertFrom-Json 
        $Electronewreleases = $genrecontent.containers.'container-album-new-releases'
        $Electroalbumnewreleases = $Electronewreleases.albums.items
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        
        foreach($Album in $Electroalbumnewreleases){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name.subString(0,[System.Math]::Min(40, $Album.artist.name.Length))) - $($Album.title.subString(0, [System.Math]::Min(40, $Album.title.Length)))"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster ="$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "ELECTRO"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
}

# code to run when Search button is clicked
#------------------------------------------
function SearchArtist  {
    if ($QobuzUrlRadiobutton.IsChecked){
    if ($ArtistRadiobutton.IsChecked)
    {
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $BaseUri = "https://www.qobuz.com/api.json/0.2/catalog/search?=%22artist%22%3A&=%22id%22%3A&query="
        $urlapi = "$($BaseUri)$($searchout)&offset=0&offset=0&limit=25&limit=25&extra=albums&format_id=$($QobuzToken)&app_id=$($Qobuzappid)" 
        $ArtistList = Invoke-WebRequest -Uri $urlapi -UseBasicParsing -Method GET -Headers $headers
        $ArtistListjson = $ArtistList |ConvertFrom-Json
        $responsejsonalbums = $ArtistListjson.albums |ConvertTo-Json -Depth 9
        $jresponsejsonalbums = $responsejsonalbums | Select-Object($_.title) | ConvertFrom-Json
        $responseitems = $jresponsejsonalbums.items 
        $qobuzmainlink = 'http://open.qobuz.com/album/'
        foreach($Album in $responseitems){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name) - $($Album.title.subString(0, [System.Math]::Min(60, $Album.title.Length))) ( $($Album.maximum_bit_depth)/$($Album.maximum_sampling_rate) )"
                QobuzUrl ="$($qobuzmainlink)$($album.id)"
                QobuzPoster = "$($Album.image.large)"
                Qposterlabel = "$($Album.artist.name) - $($Album.title)"
                Genrelabel = "ARTIST ALBUMS"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($SongsRadiobutton.IsChecked) 
    {
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $BaseUri = "https://www.qobuz.com/api.json/0.2/catalog/search?query="
        $urltrackapi = "$($BaseUri)$($searchout)&offset=0&limit=25&extra=track&format_id=$($QobuzToken)&app_id=$($Qobuzappid)" 
        $SongsData = Invoke-WebRequest -Uri $urltrackapi -UseBasicParsing -Method GET -Headers $headers
        $Songslist = $SongsData |ConvertFrom-Json
        $tracks = $Songslist.tracks 
        $tracksitems = $tracks | Select-Object ($_.items) 
        $qobuztracklink = 'http://open.qobuz.com/track/'
        foreach($Track in $tracksitems.items){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Track.performer.name.subString(0,[System.Math]::Min(17, $Track.performer.name.Length))) - $($Track.title.subString(0, [System.Math]::Min(30, $Track.title.Length)))"
                QobuzUrl ="$($qobuztracklink)$($Track.id)"
                QobuzPoster ="$($Track.album.image.large)"
                Qposterlabel = "$($Track.artist.name) - $($Track.title)"
                Genrelabel = "SONGS"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($PlaylistRadiobutton.IsChecked){
        $Albums_Listbox.Items.Clear()
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $authorization=@{}
        $authorization.Add("X-User-Auth-Token", "$($QobuzToken)")
        $authorization.Add("X-App-Id", "$($Qobuzappid)")
        $Playlistresponse = Invoke-RestMethod -Uri "https://www.qobuz.com/api.json/0.2/playlist/search?query=$($Search_Box.Text)" -UseBasicParsing -Method GET -Headers $authorization
        $Playlistitems = $Playlistresponse.playlists.items
        $Playlisturlbase = "https://play.qobuz.com/playlist/"
        foreach($Track in $Playlistitems){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Track.name)"
                QobuzUrl ="$($Playlisturlbase)$($Track.id)"
                QobuzPoster ="$($Track.images300[0])"
                Qposterlabel = "$($Track.name)"
                Genrelabel = "PLAYLIST"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
    }

  }
}

# code to run when Download button is clicked
#-----------------------------------
function URL  {
    if ($UrlRadiobutton.IsChecked) 
    {   
        ###### Variables for Paths ######
        $Trackpath = New-Item "dHJhY2tsaXN0.dat"
        ######getting a new authorization code######
        
        $headers=@{}
        $headers.Add("Authorization", "Bearer BQAtHMN8DlrTvmFczP8Ut5uN1vJSieLGphmV_CP6B-v8c9DhsTmR63llKWN94tUe-cF9SGl4SaPExM0fehTFg6C1_0yO4MeFyd6mDzo19xTzunK7StkdBUt3b1YmFTNwAOs7ROCuNrAHmw0iwLbupxC8zXV5yZ75hbGGU7XHjRS8J7cwEDKCbAT-IVl6XeSvWaHAk0Jk0DAiXNJUb47bwNFo3G4S-aSFrUcAQIaRAU5ZhitTYI7PNmVVbz8srnIfnPOaE5b_A7n_S2gmXMwF8yTOJmaDpQ")
        $responserefreshtoken = Invoke-WebRequest -Uri 'https://spotlistr.com/.netlify/functions/routes/refresh_token?refresh_token=AQBdwWzyXVNeyusHAv-v-47Ri0Q-hws15Q6aStpzd9KT_hXXEOYnFhPayYvowfhazIbOZch-aChlSdL3Zt0Ju4LIctfV--7pnkIVHYs2buO4QXEJqeKHmlWnb7H5c8zdRRs' -UseBasicParsing -Method GET -Headers $headers
        $freshtoken = $responserefreshtoken.Content
        $freshtokenjson = $freshtoken | ConvertFrom-Json 
        $spotifytoken = $freshtokenjson.access_token
        
        ####search for playlists#######
        
        
        $playlistcode = (Write-Output $Search_Box.Text) -replace "https://open.spotify.com/playlist/" -replace ([Regex]::Escape("[https://open.spotify.com/playlist/]")),"" 
        
        #######spotify api script starts here######
        
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $headers.Add("Authorization",(Write-Output "Bearer $spotifytoken"))
        $response = Invoke-RestMethod -Uri "https://api.spotify.com/v1/playlists/$playlistcode/?fields=fields%3Dhref%2Cname%2Cowner(!href%2Cexternal_urls)%2Ctracks.items(added_by.id%2Ctrack(name%2Chref%2Calbum(name%2Chref)))" -UseBasicParsing -Method GET -Headers $headers 
        $PlaylistName = $response.name
        $playlistcover = $response.images.url
        $responseheaders = $response.tracks.items | ConvertTo-Json -Depth 9
        $responseitem = $responseheaders
        $responseChars = $responseitem
        $responseCharsjson = $responseChars | ConvertFrom-Json
        $responsetracks = $responseCharsjson.track
        $artist = $responsetracks.artists
        $artistname = $artist.name
        $Songtitle = $responsetracks.name
        Write-Host "`n"
        Write-Host '---==={Starting Conversion}===---' -Foregroundcolor Blue
        Write-Host "`n"
        Write-Host 'Playlist Name:'"$PlaylistName" -Foregroundcolor Green
        Write-Host "`n"
        foreach ($line in ($Songtitle))
        {$line | Out-Host}
        $regexisrc = $responsetracks.external_ids -replace '[@={},]' -replace '\@{},\+',''
        $isrcresolve = $regexisrc -replace "isrc" -replace ([Regex]::Escape("[isrc]")),""
        foreach ($line in ($isrcresolve)| Where-Object { $_ -match '\S' })
        {  
        (Write-Output "$line") | Select-Object | Add-Content $Trackpath 
        }
        $Playlistpath = New-Item "cGxheWxpc3RuYW1l.dat"
        Write-Host "`n"
        Write-Host '---==={Please Wait}===---' -Foregroundcolor Red
        Write-Host "`n"
        foreach ($line in [System.IO.File]:: ReadLines($Trackpath)) 
        {   
        $search = $line
        $Searchregex = $search -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output $Searchregex)
        $headers=@{}
        $headers.Add("Content-Type", "application/json")
        $urltrackapi = "https://www.qobuz.com/api.json/0.2/catalog/search?query=$searchout&offset=0&limit=1&extra=track&format_id=$QobuzToken&app_id=$Qobuzappid" 
        $response = Invoke-WebRequest -Uri $urltrackapi -UseBasicParsing -Method GET -Headers $headers -ErrorAction SilentlyContinue
        $responsejson = $response |ConvertFrom-Json
        $tracks = $responsejson.tracks 
        $tracksitems = $tracks | Select-Object ($_.items) 
        $qobuztracklink = 'http://open.qobuz.com/track/'
        $tracknumber = $tracksitems.items.track_number
        $performername = $tracksitems.items.performer.name
        $albumtitle = $tracksitems.items.album.title
        $trackname = $tracksitems.items.title
        $trackversion = foreach ($item in $tracksitems.items.version) {' ('+$item+')'}
        $trackresolved = $tracksitems.items | Select-Object @{Label=',';Expression={@{$true=$_.title+=':';$false=$_.title+$qobuztracklink+$_.id}[$_.title.StartsWith('http')]}} 
        $trackregex = $trackresolved -replace '[@={},]' -replace '\@{},\+','-'
        (Write-Output "$trackregex") | Select-Object | Add-Content $Playlistpath
        } 
        $Spotjobs = ".\d-fi.exe /c -q '$QQualityFiles' -b -o '$SpotMusicpath/$PlaylistName/$SpotLayoutMusic' -d -i '$Playlistpath'; Get-ChildItem -Path '$SpotMusicpath/$PlaylistName' -Filter '*.flac' -ErrorAction SilentlyContinue -Recurse | Select-Object -ExpandProperty FullName | Sort-Object | Add-Content '$SpotMusicpath/$PlaylistName.m3u8' -Encoding utf8 -Force; Remove-Item '$Trackpath' -ErrorAction SilentlyContinue; Remove-Item '$Playlistpath' -ErrorAction SilentlyContinue; Exit"
         # Start a new PowerShell process using Start-Process
         Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $Spotjobs
        Invoke-WebRequest -Uri "$playlistcover" -OutFile "$SpotMusicpath/$PlaylistName.jpg" -ErrorAction SilentlyContinue | Out-Null
        #Get-ChildItem -Path "$SpotMusicpath/$PlaylistName" -Filter '*.flac' -ErrorAction SilentlyContinue -Recurse | ForEach-Object{$_.FullName} | Add-Content "$SpotMusicpath/$PlaylistName.m3u8" -Encoding utf8 -Force
        Write-Host "`n"
        Write-Host 'Download Completed' -Foregroundcolor Green
        Write-Host "`n"
        $Search_Box.Clear()
        $Script:SpotJob=$SpotifyJobs
    }
    elseif ($DeezerUrlRadiobutton.IsChecked)
    {
        if ($DeezerUrlRadiobutton.IsChecked -eq $true)
        {
         $deezerjobs = ".\d-fi.exe /c -u '$($Albumsindex.Where({$_.Albums -eq $Albums_Listbox.SelectedItem}).QobuzUrl)' -q '$DQualityFiles' -o '$DMusicpath/$DLayoutMusic' -d; Exit"
         # Start a new PowerShell process using Start-Process
         Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $deezerjobs
         $Search_Box.Clear()
         $Script:DeezJob=$deezerjobs
        }
        else {
         $Playlistpath = New-Item "cGxheWxpc3RuYW1l.dat"
         (Write-Output $Search_Box.Text) | Select-Object | Add-Content $Playlistpath
         # Construct the command string
         $PlaylistJobs = ".\d-fi.exe /c  -q '$DQualityFiles' -o '$DMusicpath/$DLayoutMusic' -d -i '$Playlistpath'; Exit"
         # Start a new PowerShell process using Start-Process
         Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $PlaylistJobs
         Remove-Item "$Playlistpath" -ErrorAction SilentlyContinue
         $Search_Box.Clear()
         $Script:PlayJob=$PlaylistJobs
        }
    }    
    else{
         $albumsresults = $($Albumsindex.Where({$_.Albums -eq $Albums_Listbox.SelectedItem}).QobuzUrl) 

         # Construct the command string
         $Qobuzjobs = ".\d-fi.exe /c -u '$albumsresults' -q '$QQualityFiles' -d -b -o '$QMusicpath\$QLayoutMusic'; Exit"

         # Start a new PowerShell process using Start-Process
         Start-Process powershell.exe -ArgumentList "-NoExit", "-Command", $Qobuzjobs

         $Search_Box.Clear()
         $Script:QobuJob=$Qobuzjobs
    }

    
}
function DeezerSearchArtist {
    if  ($DeezerUrlRadiobutton.IsChecked){
        if ($AlbumsRadiobutton.IsChecked){
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box.Text -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $BaseUri = "https://api.deezer.com/search/album/?q="
        $WebRequest = Invoke-RestMethod -Uri "$($BaseUri)$($searchout)&index=0&limit=50&output=json" -UseBasicParsing -Method GET
        $DeezerAlbData = $WebRequest.data
        foreach($Album in $DeezerAlbData){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name) - $($Album.Title.subString(0, [System.Math]::Min(60, $Album.Title.Length)))"
                QobuzUrl ="$($Album.link)"
                QobuzPoster ="$($Album.cover_xl)"
                Qposterlabel = "$($Album.Title)"
                Genrelabel = "ARTIST ALBUMS"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
        ###Deezer Api End#####
    }
    elseif ($ArtistRadiobutton.IsChecked) {
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box.Text -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $BaseUri = "https://api.deezer.com/search/artist/?q="
        $WebRequest = Invoke-RestMethod -Uri "$($BaseUri)$($searchout)&index=0&limit=50&output=json" -UseBasicParsing -Method GET
        $DeezerAlbData = $WebRequest.data
        foreach($Album in $DeezerAlbData){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.name)"
                QobuzUrl ="$($Album.link)"
                QobuzPoster ="$($Album.picture_xl)"
                Qposterlabel = "$($Album.name)"
                Genrelabel = "ARTIST"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($SongsRadiobutton.IsChecked) {
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box.Text -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $BaseUri = "https://api.deezer.com/search/track/?q="
        $WebRequest = Invoke-RestMethod -Uri "$($BaseUri)$($searchout)&index=0&limit=50&output=json" -UseBasicParsing -Method GET
        $DeezerAlbData = $WebRequest.data
        foreach($Album in $DeezerAlbData){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.artist.name) - $($Album.title) - $($Album.album.title)"
                QobuzUrl ="$($Album.link)"
                QobuzPoster ="$($Album.album.cover_xl)"
                Qposterlabel = "$($Album.artist.name) - $($Album.Title)"
                Genrelabel = "SONGS"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
    elseif ($PlaylistRadiobutton.IsChecked) {
        $Albums_Listbox.Items.Clear()
        $Searchregex = $Search_Box.Text -replace '[^a-zA-Z0-9\s]' -replace '\s+','-'
        $searchout = (Write-Output "$Searchregex")
        $Albums_list = [System.Collections.Generic.List[PSCustomObject]]::new()
        $BaseUri = "https://api.deezer.com/search/playlist/?q="
        $WebRequest = Invoke-RestMethod -Uri "$($BaseUri)$($searchout)&index=0&limit=50&output=json" -UseBasicParsing -Method GET
        $DeezerAlbData = $WebRequest.data
        foreach($Album in $DeezerAlbData){
            $AlbnewrelResults = [PSCustomObject]@{
                Albums = "$($Album.title) - $($Album.type)"
                QobuzUrl ="$($Album.link)"
                QobuzPoster ="$($Album.picture_xl)"
                Qposterlabel = "$($Album.Title)"
                Genrelabel = "PLAYLIST"
            }
            $Albums_list.Add($AlbnewrelResults)
        }
        foreach($Album in $Albums_list){$Albums_Listbox.Items.Add($Album.Albums) | Out-Null}
        $Search_Box.Clear()
        $Script:Albumsindex=$Albums_list
        $Search_Box.Clear()
    }
  
  }

}


$Albums_Listbox.add_SelectionChanged({if({SearchNewmusic}){try{$Albums_Covers.Source = $Albumsindex.Where({$_.Albums -eq $Albums_Listbox.SelectedItem}).QobuzPoster; $Poster_Label.Content.Text = $Albumsindex.Where({$_.Albums -eq $Albums_Listbox.SelectedItem}).Qposterlabel; $AlbumLabel.Content = $Albumsindex.Genrelabel[0]} catch{$Albums_Covers.Source ="https://pngimage.net/wp-content/uploads/2018/06/no-cover-png-1.png"}}})

$Search_Button.add_click({DeezerSearchArtist})

$Search_Button.add_click({SearchArtist})

$Downloadbutton.add_click({URL})

$Search_Button.add_click({SearchNewmusic})

#$Status_Button.Add_Click({
    if($UrlRadiobutton.IsChecked){foreach($job in $SpotJob){if(Get-Job -ChildJobState Completed -HasMoreData:$false){Remove-Job -Job $job; Clear-Host; "Download Completed..." | Out-Host} elseif(Get-Job -State Running){Receive-Job -job $job -Keep} else {"NO DOWNLOAD SESSION WAS FOUND..." | Out-Host}}} 
    elseif($DeezerUrlRadiobutton.IsChecked){foreach($job in $DeezJob){if(Get-Job -ChildJobState Completed -HasMoreData:$false){Remove-Job -Job $job; Clear-Host; "Download Completed..." | Out-Host} elseif(Get-Job -State Running){Receive-Job -job $job -Keep} else {"NO DOWNLOAD SESSION WAS FOUND..." | Out-Host}}} 
    elseif ($DeezerUrlRadiobutton.IsChecked){foreach($job in $PlayJob){if(Get-Job -ChildJobState Completed -HasMoreData:$false){Remove-Job -Job $job; Clear-Host;"Download Completed..." | Out-Host} elseif(Get-Job -State Running){Receive-Job -job $job -Keep} else {"NO DOWNLOAD SESSION WAS FOUND..." | Out-Host}}}
    elseif ($QobuzUrlRadiobutton.IsChecked){foreach($job in $QobuJob){if(Get-Job -ChildJobState Completed -HasMoreData:$false){Remove-Job -Job $job; Clear-Host;"Download Completed..." | Out-Host} elseif(Get-Job -State Running){Receive-Job -job $job -Keep} else {"NO DOWNLOAD SESSION WAS FOUND..." | Out-Host}}}
    else {foreach($job in $QobuJob){if(Get-Job -ChildJobState Completed -HasMoreData:$false){Remove-Job -Job $job; Clear-Host;"Download Completed..." | Out-Host} elseif(Get-Job -State Running){Receive-Job -job $job -Keep} else {"NO DOWNLOAD SESSION WAS FOUND..." | Out-Host}}}
#})













$xamlForm.ShowDialog() | out-null