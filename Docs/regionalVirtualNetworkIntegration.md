# Regional Virtual Network integration

The bots deployed in Azure for the functional tests have configured a Virtual Network to communicate with each other to mitigate a port exhaustion issue within the application services.

Documentation about setting the regional VNet integration can be found in the [official documentation](https://docs.microsoft.com/en-us/azure/app-service/web-sites-integrate-with-vnet#regional-vnet-integration).

## VNet implementation

![image](https://user-images.githubusercontent.com/38112957/124790114-46d1c700-df21-11eb-8884-a3591709ca81.png)

As you can see in the image, the Virtual Network resource is configured with 3 subnets, one for each language. All of these are integrated into the corresponding Bot's app service to make use of the available IPs for that subnet.
Whenever a bot wants to communicate with another bot, the virtual network will recognize the destination address as internal to the VNET and [will route the traffic through it](https://docs.microsoft.com/en-us/azure/virtual-network/virtual-networks-udr-overview). All subnets are interconnected with each other through the VNET. This brings the benefit of increased speed and available ports for connections between the bots. All outbound connections are routed through SNAT to the internet.

![image](https://user-images.githubusercontent.com/38112957/124790693-c19ae200-df21-11eb-8d45-cf70ad57b4ca.png "VNET integration example")

This configuration lowers the experienced networking issues by reducing the amount of SNAT ports needed to communicate between bots. SNAT ports are managed by Azure and [provided in batches of 128](https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-intermittent-outbound-connection-errors#cause). When a resource requires more ports than it currently has, azure auto-provisions another batch for said resource. This process is not instantaneous and, [some connections that are waiting for an available port might fail](https://azure.microsoft.com/de-de/blog/azure-load-balancer-becomes-more-efficient/). When routing the traffic inside Azure with VNET, the resources gain access to all available ports in the running OS (65.000+), increasing reliability.
